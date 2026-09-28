using System.Linq.Expressions;
using ITBees.Interfaces.Repository;
using ITBees.Models.Users;
using ITBees.RestfulApiControllers.Exceptions;
using ITBees.TicketSupport.Abstractions;
using ITBees.TicketSupport.Controllers.Models;
using ITBees.TicketSupport.Configuration;
using ITBees.TicketSupport.DbModels;
using ITBees.TicketSupport.Interfaces;
using ITBees.TicketSupport.Services;
using ITBees.UserManager.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace ITBees.TicketSupport.Tests;

public class SupportTicketWorkflowTests
{
    private Store<SupportTicket> _tickets = null!;
    private Store<SupportTicketMessage> _messages = null!;
    private Store<SupportTicketEvent> _events = null!;
    private Store<SupportTicketAttachment> _attachments = null!;
    private Store<SupportTicketRating> _ratings = null!;
    private SupportTicketRatingService _ratingService = null!;
    private SupportTicketRequesterRatingService _requesterRatingService = null!;
    private List<SupportTicketContextVm> _contexts = null!;
    private Mock<IAspCurrentUserService> _currentUser = null!;
    private Mock<ISupportTicketDeskAccess> _desk = null!;
    private Mock<ISupportTicketRequesterResolver> _resolver = null!;
    private SupportTicketService _service = null!;
    private SupportTicketQueryService _query = null!;
    private SupportTicketRequesterClosureService _closureService = null!;
    private SupportTicketStatisticsService _statistics = null!;
    private CurrentUser _requester = null!;

    [SetUp]
    public void Setup()
    {
        _tickets = new(); _messages = new(); _events = new(); _attachments = new(); _ratings = new();
        _currentUser = new(); _desk = new();
        _requester = new CurrentUser { Guid = Guid.NewGuid(), Email = "operator@example.test", DisplayName = "Operator" };
        _currentUser.Setup(x => x.GetCurrentUser()).Returns(() => _requester);
        _currentUser.Setup(x => x.GetCurrentUserGuid()).Returns(() => _requester?.Guid);
        _contexts = new();
        var contextAccess = new Mock<ISupportTicketContextAccess>();
        contextAccess.Setup(x => x.GetAvailableContexts()).Returns(() => _contexts);
        contextAccess.Setup(x => x.CheckAccess(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<bool>()))
            .Returns((string type, Guid guid, bool _) => _contexts.FirstOrDefault(x => x.Type == type && x.Guid == guid)
                ?? throw new FasApiErrorException("Context forbidden", 403));
        var requesterAccess = new SupportTicketRequesterAccess(_currentUser.Object, contextAccess.Object);
        _desk.Setup(x => x.CheckDeskAccess()).Callback(() =>
        {
            if (!_desk.Object.IsDeskUser()) throw new FasApiErrorException("Forbidden", 403);
        });
        var resolver = _resolver = new Mock<ISupportTicketRequesterResolver>();
        resolver.Setup(x => x.ResolveNames(It.IsAny<IReadOnlyCollection<Guid>>())).Returns(new Dictionary<Guid, string>());
        var writer = new SupportTicketWriter(_tickets.Write.Object, _tickets.Read.Object, _messages.Write.Object,
            _events.Write.Object, Mock.Of<ISupportTicketNumberGenerator>(x => x.Next() == 100001), NullLogger<SupportTicketWriter>.Instance);
        var mapper = new SupportTicketViewMapper(_messages.Read.Object, _attachments.Read.Object, _ratings.Read.Object,
            _events.Read.Object, resolver.Object);
        _ratingService = new SupportTicketRatingService(_ratings.Read.Object, _ratings.Write.Object,
            Mock.Of<ISupportTicketNotifier>(), writer, new SupportTicketConfiguration());
        _requesterRatingService = new SupportTicketRequesterRatingService(_tickets.Read.Object, _currentUser.Object,
            _ratingService, mapper, requesterAccess);
        _service = new SupportTicketService(_tickets.Write.Object, writer, mapper, _desk.Object, resolver.Object,
            _ratingService, Mock.Of<ISupportTicketNotifier>(), _currentUser.Object, requesterAccess);
        _query = new SupportTicketQueryService(_tickets.Read.Object, _events.Read.Object, _desk.Object,
            resolver.Object, mapper, _currentUser.Object, requesterAccess);
        _closureService = new SupportTicketRequesterClosureService(_tickets.Write.Object, writer, mapper, requesterAccess,
            _ratingService, Mock.Of<ISupportTicketNotifier>(), _currentUser.Object);
        _statistics = new SupportTicketStatisticsService(_tickets.Read.Object, _ratings.Read.Object, _desk.Object, resolver.Object);
    }

    [Test]
    public void OperatorCreatesTicketWithPhotoTableAndOwnIdentity()
    {
        var result = _service.Create(new SupportTicketIm
        {
            Subject = "Awaria", RequesterEmail = "forged@example.test",
            MessageHtml = "<p>Awaria</p><table><tbody><tr><td>Brama</td></tr></tbody></table><img src='data:image/png;base64,iVBORw0KGgo='>"
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.RequesterGuid, Is.EqualTo(_requester.Guid));
            Assert.That(result.RequesterEmail, Is.EqualTo(_requester.Email));
            Assert.That(result.Messages.Single().BodyHtml, Does.Contain("<table>").And.Contain("data:image/png"));
            Assert.That(result.Events.Single().EventType, Is.EqualTo(SupportTicketEventTypes.Created));
        });
    }

    [Test]
    public void DeskUserOnTheRequesterEndpointFilesUnderOwnIdentity()
    {
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        var result = _service.Create(new SupportTicketIm
            { Subject = "Awaria", Message = "Brama", RequesterEmail = "somebody@example.test", RequesterName = "Somebody" });
        Assert.Multiple(() =>
        {
            Assert.That(result.RequesterGuid, Is.EqualTo(_requester.Guid));
            Assert.That(result.RequesterEmail, Is.EqualTo(_requester.Email));
            Assert.That(result.Channel, Is.EqualTo(SupportTicketChannel.Panel));
        });
        _resolver.Verify(x => x.ResolveByEmail(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void DeskEntersATicketForSomebodyElse()
    {
        var caller = new SupportTicketPerson { Guid = Guid.NewGuid(), Email = "caller@example.test", DisplayName = "Caller" };
        _resolver.Setup(x => x.ResolveByEmail("caller@example.test")).Returns(caller);
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        var result = _service.CreateFromDesk(new SupportTicketIm
            { Subject = "Telefon", Message = "Nie działa brama", RequesterEmail = " caller@example.test " });
        Assert.Multiple(() =>
        {
            Assert.That(result.RequesterGuid, Is.EqualTo(caller.Guid));
            Assert.That(result.RequesterEmail, Is.EqualTo(caller.Email));
            Assert.That(result.Channel, Is.EqualTo(SupportTicketChannel.Phone));
        });
    }

    [Test]
    public void OnlyTheDeskEntersTicketsForSomebodyElse()
    {
        Assert.Throws<FasApiErrorException>(() => _service.CreateFromDesk(new SupportTicketIm
            { Subject = "Telefon", Message = "Brama", RequesterEmail = "victim@example.test" }));
        Assert.That(_tickets.Rows, Is.Empty);
        _resolver.Verify(x => x.ResolveByEmail(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void EmptyRichTextDoesNotLeaveAnOrphanTicket()
    {
        Assert.Throws<FasApiErrorException>(() => _service.Create(new SupportTicketIm { Subject = "Awaria", MessageHtml = "<p><br></p>" }));
        Assert.That(_tickets.Rows, Is.Empty);
    }

    [Test]
    public void OtherOperatorCannotReadReplyOrClose()
    {
        var ticket = Create();
        _requester = new CurrentUser { Guid = Guid.NewGuid() };
        Assert.Throws<FasApiErrorException>(() => _query.GetDetails(ticket.Guid));
        Assert.Throws<FasApiErrorException>(() => _service.ReplyAsRequester(new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Hi" }));
        Assert.Throws<FasApiErrorException>(() => _service.Close(new SupportTicketCloseIm { SupportTicketGuid = ticket.Guid }));
        Assert.That(_messages.Rows, Has.Count.EqualTo(1));
    }

    [Test]
    public void AnonymousCannotReadUnlinkedTicket()
    {
        var ticket = Create();
        _tickets.Rows.Single().RequesterGuid = null;
        _requester = null!;
        Assert.Throws<FasApiErrorException>(() => _query.GetDetails(ticket.Guid));
        Assert.Throws<FasApiErrorException>(() => _service.ReplyAsRequester(new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Hi" }));
    }

    [Test]
    public void RequesterNeverSeesInternalNoteOrItsAttachment()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.AddInternalNote(new SupportTicketNoteIm { SupportTicketGuid = ticket.Guid, Message = "Private diagnostic information" });
        var note = _messages.Rows.Last();
        _attachments.Rows.Add(new SupportTicketAttachment { Guid = Guid.NewGuid(), SupportTicketGuid = ticket.Guid, SupportTicketMessageGuid = note.Guid, StorageKey = "private-file" });
        _desk.Setup(x => x.IsDeskUser()).Returns(false);
        var result = _query.GetDetails(ticket.Guid);
        Assert.Multiple(() =>
        {
            Assert.That(result.Messages, Has.Count.EqualTo(1));
            Assert.That(result.Attachments, Is.Empty);
            Assert.That(result.Events.Select(x => x.EventType), Does.Not.Contain(SupportTicketEventTypes.NoteAdded));
        });
    }

    [Test]
    public void RequesterSeesTheNameButNotTheAddressOfTheSupportAccount()
    {
        var ticket = Create();
        var requester = _requester;
        _requester = new CurrentUser { Guid = Guid.NewGuid(), Email = "agent@example.test", DisplayName = "Agent" };
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.ReplyAsAgent(new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Sprawdzamy" });
        Assert.That(_query.GetDetails(ticket.Guid).Messages.Last().AuthorEmail, Is.EqualTo("agent@example.test"));

        _requester = requester;
        _desk.Setup(x => x.IsDeskUser()).Returns(false);
        var messages = _query.GetDetails(ticket.Guid, true).Messages;
        Assert.Multiple(() =>
        {
            Assert.That(messages.Last().AuthorName, Is.EqualTo("Agent"));
            Assert.That(messages.Last().AuthorEmail, Is.Null);
            Assert.That(messages.First().AuthorEmail, Is.EqualTo(requester.Email));
        });
    }

    [Test]
    public void DeskReplyAndCloseAreRecordedOnceAndTheRequesterCannotAnswerAClosedTicket()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        var reply = _service.ReplyAsAgent(new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Sprawdziliśmy bramę" });
        Assert.That(reply.Status, Is.EqualTo(SupportTicketStatus.WaitingForCustomer));
        Assert.That(reply.FirstResponseUtc, Is.Not.Null);
        var closed = _service.Close(new SupportTicketCloseIm
            { SupportTicketGuid = ticket.Guid, CloseReason = SupportTicketCloseReason.Spam, CloseNote = "Naprawiono", AskForRating = false });
        Assert.That(closed.Status, Is.EqualTo(SupportTicketStatus.Closed));
        _service.Close(new SupportTicketCloseIm { SupportTicketGuid = ticket.Guid, AskForRating = false });
        Assert.That(_events.Rows.Count(x => x.EventType == SupportTicketEventTypes.Closed), Is.EqualTo(1));

        _desk.Setup(x => x.IsDeskUser()).Returns(false);
        var refused = Assert.Throws<FasApiErrorException>(() => _service.ReplyAsRequester(
            new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Nadal nie działa" }));
        var row = _tickets.Rows.Single();
        Assert.Multiple(() =>
        {
            Assert.That(refused!.FasApiErrorVm.StatusCode, Is.EqualTo(409));
            Assert.That(row.Status, Is.EqualTo(SupportTicketStatus.Closed));
            Assert.That(row.CloseReason, Is.EqualTo(SupportTicketCloseReason.Spam));
            Assert.That(row.CloseNote, Is.EqualTo("Naprawiono"));
            Assert.That(row.ClosedByGuid, Is.EqualTo(_requester.Guid));
            Assert.That(row.ReopenCount, Is.Zero);
            Assert.That(_messages.Rows, Has.Count.EqualTo(2));
            Assert.That(_events.Rows.Any(x => x.EventType == SupportTicketEventTypes.Reopened), Is.False);
        });
    }

    [Test]
    public void RequesterAnswerReopensAResolvedTicket()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.ReplyAsAgent(new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Sprawdziliśmy bramę" });
        _service.ChangeStatus(new SupportTicketStatusUm { SupportTicketGuid = ticket.Guid, Status = SupportTicketStatus.Resolved });
        _desk.Setup(x => x.IsDeskUser()).Returns(false);

        var reopened = _service.ReplyAsRequester(new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Nadal nie działa" });
        Assert.Multiple(() =>
        {
            Assert.That(reopened.Status, Is.EqualTo(SupportTicketStatus.WaitingForAgent));
            Assert.That(reopened.ReopenCount, Is.EqualTo(1));
            Assert.That(reopened.ResolvedUtc, Is.Null);
            Assert.That(reopened.Messages, Has.Count.EqualTo(3));
            Assert.That(reopened.Events.Select(x => x.EventType), Does.Contain(SupportTicketEventTypes.Reopened));
        });
    }

    [Test]
    public void OnlyTheDeskReopensAClosedTicket()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.Close(new SupportTicketCloseIm
            { SupportTicketGuid = ticket.Guid, CloseReason = SupportTicketCloseReason.Duplicate, CloseNote = "Zob. #100000", AskForRating = false });
        _desk.Setup(x => x.IsDeskUser()).Returns(false);

        // The requester raised it, but the reopen endpoint belongs to the desk.
        Assert.Throws<FasApiErrorException>(() => _service.Reopen(new SupportTicketReopenIm
            { SupportTicketGuid = ticket.Guid, Message = "To nie duplikat" }));
        Assert.That(_tickets.Rows.Single().Status, Is.EqualTo(SupportTicketStatus.Closed));
        Assert.That(_tickets.Rows.Single().CloseNote, Is.EqualTo("Zob. #100000"));
        Assert.That(_messages.Rows, Has.Count.EqualTo(1));

        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        var reopened = _service.Reopen(new SupportTicketReopenIm { SupportTicketGuid = ticket.Guid, Message = "Jednak nie duplikat" });
        Assert.Multiple(() =>
        {
            Assert.That(reopened.Status, Is.EqualTo(SupportTicketStatus.Open));
            Assert.That(reopened.ReopenCount, Is.EqualTo(1));
            Assert.That(reopened.CloseReason, Is.Null);
            Assert.That(_messages.Rows.Last().Direction, Is.EqualTo(SupportTicketMessageDirection.InternalNote));
            Assert.That(_events.Rows.Last().EventType, Is.EqualTo(SupportTicketEventTypes.Reopened));
            Assert.That(_events.Rows.Last().FromValue, Is.EqualTo(nameof(SupportTicketStatus.Closed)));
        });
    }

    [Test]
    public void DeskCannotRecordItsClosureAsTheRequesters()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        Assert.Throws<FasApiErrorException>(() => _service.Close(new SupportTicketCloseIm
            { SupportTicketGuid = ticket.Guid, CloseReason = SupportTicketCloseReason.ClosedByRequester }));
        Assert.That(_tickets.Rows.Single().Status, Is.EqualTo(SupportTicketStatus.New));
        Assert.That(_events.Rows.Any(x => x.EventType == SupportTicketEventTypes.Closed), Is.False);
    }

    private SupportTicketVm Create() => _service.Create(new SupportTicketIm { Subject = "Awaria", Message = "Brama nie działa" });

    [Test]
    public void ListsCountPublicMessagesWithoutLoadingThem()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.ReplyAsAgent(new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Sprawdzamy" });
        _service.AddInternalNote(new SupportTicketNoteIm { SupportTicketGuid = ticket.Guid, Message = "Wewnętrzne" });
        _messages.Read.Invocations.Clear();

        var queue = _query.GetForDesk(new SupportTicketListFilter());
        Assert.That(queue.Data.Single().MessageCount, Is.EqualTo(2));
        _messages.Read.Verify(x => x.GetData(It.IsAny<Expression<Func<SupportTicketMessage, bool>>>(),
            It.IsAny<Expression<Func<SupportTicketMessage, object>>[]>()), Times.Never);
    }

    [Test]
    public void AHugePageNumberGivesAnEmptyPage()
    {
        Create();
        var page = _query.GetMine(new SupportTicketListFilter { Page = int.MaxValue, PageSize = 200 });
        Assert.That(page.AllElementsCount, Is.EqualTo(1));
        Assert.That(page.Data, Is.Empty);
    }

    [Test]
    public void MysqlTimestampsAreSerializedAsUtc()
    {
        var vm = new SupportTicketVm(new SupportTicket { CreatedUtc = new DateTime(2026, 9, 3, 10, 0, 0) });
        Assert.That(vm.CreatedUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
        Assert.That(System.Text.Json.JsonSerializer.Serialize(vm), Does.Contain("2026-09-03T10:00:00Z"));
    }

    [Test]
    public void ClosingRequestsRatingAndRequesterSubmitsScoreCommentAndHistoryWithoutReopening()
    {
        var ticket = CloseWithRating();
        var pending = _query.GetDetails(ticket.Guid);
        Assert.That(pending.Rating, Is.Not.Null);
        Assert.That(pending.Rating.Score, Is.Null);
        Assert.That(pending.Events.Select(x => x.EventType), Does.Contain(SupportTicketEventTypes.RatingRequested));

        var rated = _requesterRatingService.Create(new SupportTicketRequesterRatingIm
            { SupportTicketGuid = ticket.Guid, Score = 4, Comment = "  Dziękuję za pomoc  " });

        Assert.Multiple(() =>
        {
            Assert.That(rated.Status, Is.EqualTo(SupportTicketStatus.Closed));
            Assert.That(rated.Rating.Score, Is.EqualTo(4));
            Assert.That(rated.Rating.Comment, Is.EqualTo("Dziękuję za pomoc"));
            Assert.That(rated.Rating.RatedUtc?.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(rated.Events.Single(x => x.EventType == SupportTicketEventTypes.Rated).ToValue, Is.EqualTo("4"));
            Assert.That(_query.GetDetails(ticket.Guid).Rating.Score, Is.EqualTo(4));
        });

        Assert.Throws<FasApiErrorException>(() => _requesterRatingService.Create(new SupportTicketRequesterRatingIm
            { SupportTicketGuid = ticket.Guid, Score = 1 }));
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        Assert.That(_query.GetDetails(ticket.Guid).Rating.Comment, Is.EqualTo("Dziękuję za pomoc"));
        Assert.That(_ratings.Rows.Single().Score, Is.EqualTo(4));
    }

    [Test]
    public void OtherRequesterAndDeskCannotRateSomebodyElsesTicket()
    {
        var ticket = CloseWithRating();
        _requester = new CurrentUser { Guid = Guid.NewGuid() };
        var input = new SupportTicketRequesterRatingIm { SupportTicketGuid = ticket.Guid, Score = 5 };
        Assert.Throws<FasApiErrorException>(() => _requesterRatingService.Create(input));
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        Assert.Throws<FasApiErrorException>(() => _requesterRatingService.Create(input));
        _requester = null!;
        Assert.Throws<FasApiErrorException>(() => _requesterRatingService.Create(input));
        Assert.That(_ratings.Rows.Single().Score, Is.Null);
    }

    [TestCase(0)]
    [TestCase(6)]
    public void InvalidScoreDoesNotWriteRatingOrHistory(int score)
    {
        var ticket = CloseWithRating();
        Assert.Throws<FasApiErrorException>(() => _requesterRatingService.Create(new SupportTicketRequesterRatingIm
            { SupportTicketGuid = ticket.Guid, Score = score }));
        Assert.That(_ratings.Rows.Single().Score, Is.Null);
        Assert.That(_events.Rows.Any(x => x.EventType == SupportTicketEventTypes.Rated), Is.False);
    }

    [Test]
    public void TicketClosedWithoutRatingRequestCannotBeRated()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.Close(new SupportTicketCloseIm { SupportTicketGuid = ticket.Guid, AskForRating = false });
        Assert.Throws<ResultNotFoundException>(() => _requesterRatingService.Create(new SupportTicketRequesterRatingIm
            { SupportTicketGuid = ticket.Guid, Score = 5 }));
        Assert.That(_ratings.Rows, Is.Empty);
    }

    [Test]
    public void DeletedTicketCannotBeRated()
    {
        var ticket = CloseWithRating();
        _tickets.Rows.Single().Deleted = true;
        Assert.Throws<ResultNotFoundException>(() => _requesterRatingService.Create(new SupportTicketRequesterRatingIm
            { SupportTicketGuid = ticket.Guid, Score = 5 }));
        Assert.Throws<ResultNotFoundException>(() => _ratingService.Submit(_tickets.Rows.Single(), 5, null));
        Assert.That(_ratings.Rows.Single().Score, Is.Null);
    }

    private SupportTicketVm CloseWithRating()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        var closed = _service.Close(new SupportTicketCloseIm { SupportTicketGuid = ticket.Guid });
        _desk.Setup(x => x.IsDeskUser()).Returns(false);
        return closed;
    }

    [Test]
    public void CoworkerCanReadAndReplyToSharedTicketWithoutSeeingInternalContent()
    {
        var context = AddContext();
        var originalRequester = _requester.Guid;
        var shared = CreateShared(context);
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.AddInternalNote(new SupportTicketNoteIm { SupportTicketGuid = shared.Guid, Message = "Internal diagnostics" });
        _desk.Setup(x => x.IsDeskUser()).Returns(false);
        _requester = new CurrentUser { Guid = Guid.NewGuid(), DisplayName = "Coworker", Email = "coworker@example.test" };

        var details = _query.GetDetails(shared.Guid, true);
        Assert.That(details.Messages, Has.Count.EqualTo(1));
        Assert.That(details.ContextGuid, Is.EqualTo(context.Guid));
        Assert.That(details.ContextName, Is.EqualTo(context.Name));
        var reply = _service.ReplyAsRequester(new SupportTicketReplyIm { SupportTicketGuid = shared.Guid, Message = "Potwierdzam awarię" });
        Assert.That(reply.RequesterGuid, Is.EqualTo(originalRequester));
        Assert.That(reply.Messages.Last().AuthorName, Is.EqualTo("Coworker"));
        Assert.That(reply.Messages, Has.Count.EqualTo(2));
        Assert.That(reply.Events.Any(x => x.EventType == SupportTicketEventTypes.NoteAdded), Is.False);
    }

    [Test]
    public void VisibilityIsFilteredBeforeCountingAndPagingAndPrivateTicketsStayPrivate()
    {
        var firstContext = AddContext();
        var secondContext = AddContext();
        var firstShared = CreateShared(firstContext);
        var secondShared = CreateShared(secondContext);
        var privateTicket = Create();
        _requester = new CurrentUser { Guid = Guid.NewGuid() };
        var ownTicket = Create();
        _contexts.Remove(secondContext);

        var page = _query.GetMine(new SupportTicketListFilter { Page = 1, PageSize = 1 });
        var all = _query.GetMine(new SupportTicketListFilter { PageSize = 25 });
        Assert.That(page.AllElementsCount, Is.EqualTo(2));
        Assert.That(page.Data, Has.Count.EqualTo(1));
        Assert.That(all.Data.Select(x => x.Guid), Is.EquivalentTo(new[] { firstShared.Guid, ownTicket.Guid }));
        Assert.Throws<FasApiErrorException>(() => _query.GetDetails(secondShared.Guid, true));
        Assert.Throws<FasApiErrorException>(() => _query.GetDetails(privateTicket.Guid, true));
        Assert.Throws<FasApiErrorException>(() => _service.ReplyAsRequester(new SupportTicketReplyIm
            { SupportTicketGuid = secondShared.Guid, Message = "Forbidden" }));
    }

    [Test]
    public void RevokingContextAccessAlsoRemovesOriginalRequestersAccess()
    {
        var shared = CreateShared(AddContext());
        var personal = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.Close(new SupportTicketCloseIm { SupportTicketGuid = shared.Guid });
        _desk.Setup(x => x.IsDeskUser()).Returns(false);
        _contexts.Clear();
        Assert.That(_query.GetMine(new SupportTicketListFilter()).Data.Select(x => x.Guid), Is.EquivalentTo(new[] { personal.Guid }));
        Assert.Throws<FasApiErrorException>(() => _query.GetDetails(shared.Guid, true));
        Assert.Throws<FasApiErrorException>(() => _service.ReplyAsRequester(new SupportTicketReplyIm
            { SupportTicketGuid = shared.Guid, Message = "No access" }));
        Assert.Throws<FasApiErrorException>(() => _requesterRatingService.Create(new SupportTicketRequesterRatingIm
            { SupportTicketGuid = shared.Guid, Score = 5 }));
        Assert.That(_ratings.Rows.Single().Score, Is.Null);
    }

    [Test]
    public void SharedTicketCanOnlyBeRatedByItsSignedInRequester()
    {
        var shared = CreateShared(AddContext());
        var originalRequester = _requester;
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.Close(new SupportTicketCloseIm { SupportTicketGuid = shared.Guid });
        _desk.Setup(x => x.IsDeskUser()).Returns(false);
        Assert.That(_query.GetDetails(shared.Guid, true).CanRate, Is.True);
        var input = new SupportTicketRequesterRatingIm { SupportTicketGuid = shared.Guid, Score = 5 };
        _requester = new CurrentUser { Guid = Guid.NewGuid() };
        Assert.That(_query.GetDetails(shared.Guid, true).CanRate, Is.False);
        Assert.Throws<FasApiErrorException>(() => _requesterRatingService.Create(input));
        _requester = null!;
        Assert.Throws<FasApiErrorException>(() => _requesterRatingService.Create(input));
        Assert.That(_ratings.Rows.Single().Score, Is.Null);
        _requester = originalRequester;
        Assert.That(_requesterRatingService.Create(new SupportTicketRequesterRatingIm
            { SupportTicketGuid = shared.Guid, Score = 5 }).Rating.Score, Is.EqualTo(5));
    }

    [Test]
    public void RequesterEndpointDoesNotInheritDeskAccessToOtherPrivateTickets()
    {
        var ticket = Create();
        _requester = new CurrentUser { Guid = Guid.NewGuid() };
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        Assert.That(_query.GetDetails(ticket.Guid).Guid, Is.EqualTo(ticket.Guid));
        Assert.Throws<FasApiErrorException>(() => _query.GetDetails(ticket.Guid, true));
        Assert.Throws<FasApiErrorException>(() => _service.ReplyAsRequester(new SupportTicketReplyIm
            { SupportTicketGuid = ticket.Guid, Message = "Not the requester" }));
    }

    [TestCase("parking", null)]
    [TestCase(null, "11111111-1111-1111-1111-111111111111")]
    [TestCase("", "11111111-1111-1111-1111-111111111111")]
    [TestCase("parking", "00000000-0000-0000-0000-000000000000")]
    [TestCase("unknown", "11111111-1111-1111-1111-111111111111")]
    [TestCase("parking", "11111111-1111-1111-1111-111111111111")]
    public void InvalidOrUnauthorizedContextDoesNotCreateTicket(string? type, string? guid)
    {
        Assert.Throws<FasApiErrorException>(() => _service.Create(new SupportTicketIm
            { Subject = "Test", Message = "Test", ContextType = type, ContextGuid = guid == null ? null : Guid.Parse(guid) }));
        Assert.That(_tickets.Rows, Is.Empty);
    }

    [Test]
    public void ContextAdapterAnsweringNullOrAnotherContextIsADenial()
    {
        var contextGuid = Guid.NewGuid();
        var adapter = new Mock<ISupportTicketContextAccess>();
        var access = new SupportTicketRequesterAccess(_currentUser.Object, adapter.Object);
        var shared = new SupportTicket
            { Guid = Guid.NewGuid(), ContextType = "parking", ContextGuid = contextGuid, RequesterGuid = Guid.NewGuid() };

        adapter.Setup(x => x.CheckAccess("parking", contextGuid, It.IsAny<bool>())).Returns((SupportTicketContextVm)null!);
        Assert.Throws<FasApiErrorException>(() => access.Check(shared, false));
        Assert.Throws<FasApiErrorException>(() => access.CheckContext("parking", contextGuid, true));

        adapter.Setup(x => x.CheckAccess("parking", contextGuid, It.IsAny<bool>()))
            .Returns(new SupportTicketContextVm("parking", Guid.NewGuid(), "Another parking"));
        Assert.Throws<FasApiErrorException>(() => access.Check(shared, false));

        adapter.Setup(x => x.CheckAccess("parking", contextGuid, It.IsAny<bool>()))
            .Returns(new SupportTicketContextVm("Parking", contextGuid, "Test parking"));
        Assert.DoesNotThrow(() => access.Check(shared, true));
    }

    [Test]
    public void DefaultContextAdapterDeniesSharedTickets()
    {
        var adapter = new PrivateSupportTicketContextAccess();
        Assert.That(adapter.GetAvailableContexts(), Is.Empty);
        Assert.Throws<FasApiErrorException>(() => adapter.CheckAccess("parking", Guid.NewGuid(), false));
    }

    private SupportTicketContextVm AddContext()
    {
        var context = new SupportTicketContextVm("parking", Guid.NewGuid(), "Test parking");
        _contexts.Add(context);
        return context;
    }

    private SupportTicketVm CreateShared(SupportTicketContextVm context) => _service.Create(new SupportTicketIm
        { Subject = "Awaria parkingu", Message = "Brama", ContextType = context.Type, ContextGuid = context.Guid });

    [Test]
    public void RequesterClosesAnsweredTicketAndIsOfferedTheRating()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.ReplyAsAgent(new SupportTicketReplyIm { SupportTicketGuid = ticket.Guid, Message = "Naprawione" });
        _desk.Setup(x => x.IsDeskUser()).Returns(false);

        var closed = _closureService.Create(new SupportTicketRequesterClosureIm
            { SupportTicketGuid = ticket.Guid, CloseNote = "  Działa, dziękuję  " });

        Assert.Multiple(() =>
        {
            Assert.That(closed.Status, Is.EqualTo(SupportTicketStatus.Closed));
            Assert.That(closed.CloseReason, Is.EqualTo(SupportTicketCloseReason.ClosedByRequester));
            Assert.That(closed.CloseNote, Is.EqualTo("Działa, dziękuję"));
            Assert.That(closed.ClosedByGuid, Is.EqualTo(_requester.Guid));
            Assert.That(closed.CanRate, Is.True);
            Assert.That(closed.Events.Last(x => x.EventType == SupportTicketEventTypes.Closed).ActorGuid, Is.EqualTo(_requester.Guid));
            Assert.That(_tickets.Rows.Single().Status, Is.EqualTo(SupportTicketStatus.Closed));
        });

        // Closing again changes nothing and writes no second closure.
        _closureService.Create(new SupportTicketRequesterClosureIm { SupportTicketGuid = ticket.Guid });
        Assert.That(_events.Rows.Count(x => x.EventType == SupportTicketEventTypes.Closed), Is.EqualTo(1));
        Assert.That(_requesterRatingService.Create(new SupportTicketRequesterRatingIm
            { SupportTicketGuid = ticket.Guid, Score = 5 }).Rating.Score, Is.EqualTo(5));
    }

    [Test]
    public void UnansweredTicketClosedByRequesterAsksForNoRating()
    {
        var ticket = Create();
        var closed = _closureService.Create(new SupportTicketRequesterClosureIm { SupportTicketGuid = ticket.Guid });
        Assert.That(closed.Status, Is.EqualTo(SupportTicketStatus.Closed));
        Assert.That(closed.CanRate, Is.False);
        Assert.That(_ratings.Rows, Is.Empty);
    }

    [Test]
    public void OnlySomebodyWhoMayWriteToTheTicketCanCloseIt()
    {
        var personal = Create();
        var shared = CreateShared(AddContext());
        _requester = new CurrentUser { Guid = Guid.NewGuid(), DisplayName = "Coworker" };

        Assert.Throws<FasApiErrorException>(() => _closureService.Create(new SupportTicketRequesterClosureIm
            { SupportTicketGuid = personal.Guid }));
        Assert.That(_closureService.Create(new SupportTicketRequesterClosureIm { SupportTicketGuid = shared.Guid }).Status,
            Is.EqualTo(SupportTicketStatus.Closed));

        _requester = null!;
        Assert.Throws<FasApiErrorException>(() => _closureService.Create(new SupportTicketRequesterClosureIm
            { SupportTicketGuid = personal.Guid }));
        Assert.That(_tickets.Rows.Single(x => x.Guid == personal.Guid).Status, Is.EqualTo(SupportTicketStatus.New));
    }

    [Test]
    public void TicketsClosedByRequestersDoNotCountForTheDesk()
    {
        var agent = new CurrentUser { Guid = Guid.NewGuid(), DisplayName = "Agent" };
        var operatorUser = _requester;
        var byDesk = Create();
        var byRequester = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _requester = agent;
        _service.Close(new SupportTicketCloseIm { SupportTicketGuid = byDesk.Guid, AskForRating = false });
        _requester = operatorUser;
        _desk.Setup(x => x.IsDeskUser()).Returns(false);
        _closureService.Create(new SupportTicketRequesterClosureIm { SupportTicketGuid = byRequester.Guid });

        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        var statistics = _statistics.Get(null, null);
        Assert.That(statistics.ClosedCount, Is.EqualTo(2));
        Assert.That(statistics.Agents.Single().AgentGuid, Is.EqualTo(agent.Guid));
        Assert.That(statistics.Agents.Single().ClosedCount, Is.EqualTo(1));
    }

    [Test]
    public void HostReferenceIsStoredAndVisibleOnTheTicket()
    {
        var created = _service.Create(new SupportTicketIm { Subject = "Analiza sesji", Message = "Szlaban" },
            new SupportTicketReference("parking_session", "477660"));
        Assert.Multiple(() =>
        {
            Assert.That(created.ReferenceType, Is.EqualTo("parking_session"));
            Assert.That(created.ReferenceId, Is.EqualTo("477660"));
            Assert.That(_tickets.Rows.Single().ReferenceId, Is.EqualTo("477660"));
            Assert.That(_query.GetMine(new SupportTicketListFilter()).Data.Single().ReferenceType, Is.EqualTo("parking_session"));
        });
    }

    [TestCase("parking_session", null)]
    [TestCase(null, "477660")]
    [TestCase(" ", "477660")]
    public void IncompleteReferenceDoesNotCreateTicket(string? type, string? id)
    {
        Assert.Throws<FasApiErrorException>(() => _service.Create(new SupportTicketIm { Subject = "Analiza", Message = "Test" },
            new SupportTicketReference(type!, id!)));
        Assert.That(_tickets.Rows, Is.Empty);
    }

    [Test]
    public void TrustedCallerNoteStaysOnTheDeskAndReplyReachesTheRequester()
    {
        var ticket = Create();
        _service.ReplyAsTrustedCaller(new SupportTicketTrustedReplyCommand
            { SupportTicketGuid = ticket.Guid, AuthorName = "Octopark AI", Message = "Root cause: loop 2 stuck", InternalNote = true });
        var afterNote = _tickets.Rows.Single();
        Assert.That(afterNote.Status, Is.EqualTo(SupportTicketStatus.New));
        Assert.That(afterNote.FirstResponseUtc, Is.Null);

        var answered = _service.ReplyAsTrustedCaller(new SupportTicketTrustedReplyCommand
            { SupportTicketGuid = ticket.Guid, AuthorName = "Octopark AI", Message = "Szlaban otworzył się po 3 s." });
        Assert.Multiple(() =>
        {
            Assert.That(answered.Status, Is.EqualTo(SupportTicketStatus.WaitingForCustomer));
            Assert.That(answered.FirstResponseUtc, Is.Not.Null);
            Assert.That(answered.Messages.Select(x => x.Direction), Is.EqualTo(new[]
            {
                SupportTicketMessageDirection.Inbound, SupportTicketMessageDirection.InternalNote,
                SupportTicketMessageDirection.Outbound
            }));
            Assert.That(answered.Messages.Last().AuthorName, Is.EqualTo("Octopark AI"));
            Assert.That(answered.Events.Select(x => x.EventType), Does.Contain(SupportTicketEventTypes.NoteAdded)
                .And.Contain(SupportTicketEventTypes.Replied));
        });

        var requesterView = _query.GetDetails(ticket.Guid, true);
        Assert.That(requesterView.Messages.Select(x => x.Body), Does.Not.Contain("Root cause: loop 2 stuck"));
        Assert.That(requesterView.Messages.Last().Body, Is.EqualTo("Szlaban otworzył się po 3 s."));
    }

    [Test]
    public void TrustedCallerCannotAnswerAClosedTicketOrStayAnonymous()
    {
        var ticket = Create();
        _desk.Setup(x => x.IsDeskUser()).Returns(true);
        _service.Close(new SupportTicketCloseIm { SupportTicketGuid = ticket.Guid, AskForRating = false });
        Assert.Throws<FasApiErrorException>(() => _service.ReplyAsTrustedCaller(new SupportTicketTrustedReplyCommand
            { SupportTicketGuid = ticket.Guid, AuthorName = "Octopark AI", Message = "Too late" }));
        Assert.Throws<FasApiErrorException>(() => _service.ReplyAsTrustedCaller(new SupportTicketTrustedReplyCommand
            { SupportTicketGuid = ticket.Guid, Message = "Nameless", InternalNote = true }));
        Assert.That(_messages.Rows, Has.Count.EqualTo(1));

        // A note on a finished ticket is still allowed - the desk may want the analysis on record.
        _service.ReplyAsTrustedCaller(new SupportTicketTrustedReplyCommand
            { SupportTicketGuid = ticket.Guid, AuthorName = "Octopark AI", Message = "Post-mortem", InternalNote = true });
        Assert.That(_tickets.Rows.Single().Status, Is.EqualTo(SupportTicketStatus.Closed));
    }

    private sealed class Store<T> where T : class
    {
        public List<T> Rows { get; } = new();
        public Mock<IReadOnlyRepository<T>> Read { get; } = new();
        public Mock<IWriteOnlyRepository<T>> Write { get; } = new();
        public Store()
        {
            Read.Setup(x => x.GetFirst(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<T, object>>[]>()))
                .Returns((Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] _) => Rows.FirstOrDefault(predicate.Compile())!);
            Read.Setup(x => x.GetData(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Expression<Func<T, object>>[]>()))
                .Returns((Expression<Func<T, bool>> predicate, Expression<Func<T, object>>[] _) => Rows.Where(predicate.Compile()).ToList());
            Read.Setup(x => x.GetDataQueryable(It.IsAny<Expression<Func<T, bool>>>()))
                .Returns((Expression<Func<T, bool>> predicate) => Rows.AsQueryable().Where(predicate));
            Write.Setup(x => x.InsertData(It.IsAny<T>())).Returns((T value) => { Rows.Add(value); return value; });
            Write.Setup(x => x.UpdateData(It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<Action<T>>(), It.IsAny<Expression<Func<T, object>>[]>()))
                .Returns((Expression<Func<T, bool>> predicate, Action<T> change, Expression<Func<T, object>>[] _) =>
                {
                    var found = Rows.Where(predicate.Compile()).ToList(); found.ForEach(change); return found;
                });
        }
    }
}

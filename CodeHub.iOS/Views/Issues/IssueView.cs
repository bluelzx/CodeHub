using System;
using CodeFramework.iOS.Views;
using CodeHub.Core.ViewModels.Issues;
using MonoTouch.UIKit;
using CodeFramework.iOS.ViewControllers;
using MonoTouch.Dialog;
using CodeFramework.iOS.Utils;
using CodeFramework.iOS.Elements;
using System.Linq;
using System.Collections.Generic;
using CodeHub.iOS.ViewControllers;

namespace CodeHub.iOS.Views.Issues
{
    public class IssueView : ViewModelDrivenDialogViewController
    {
        protected HeaderView _header;
        protected WebElement _descriptionElement;
        protected WebElement _commentsElement;
        protected StyledStringElement _milestoneElement;
        protected StyledStringElement _assigneeElement;
        protected StyledStringElement _labelsElement;
        protected StyledStringElement _addCommentElement;
        private IHud _hud;

        public new IssueViewModel ViewModel
        {
            get { return (IssueViewModel) base.ViewModel; }
            set { base.ViewModel = value; }
        }

        public IssueView()
        {
            Root.UnevenRows = true;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            _header = new HeaderView();
            _hud = this.CreateHud();

            var content = System.IO.File.ReadAllText("WebCell/body.html", System.Text.Encoding.UTF8);
            _descriptionElement = new WebElement(content, "body", false);
            _descriptionElement.UrlRequested = ViewModel.GoToUrlCommand.Execute;
            //_descriptionElement.HeightChanged = x => Render();

            var content2 = System.IO.File.ReadAllText("WebCell/comments.html", System.Text.Encoding.UTF8);
            _commentsElement = new WebElement(content2, "comments", true);
            _commentsElement.UrlRequested = ViewModel.GoToUrlCommand.Execute;
            //_commentsElement.HeightChanged = x => Render();

            _milestoneElement = new StyledStringElement("Milestone", "No Milestone", UITableViewCellStyle.Value1) {Image = Images.Milestone, Accessory = UITableViewCellAccessory.DisclosureIndicator};
            _milestoneElement.Tapped += () => ViewModel.GoToMilestoneCommand.Execute(null);

            _assigneeElement = new StyledStringElement("Assigned", "Unassigned".t(), UITableViewCellStyle.Value1) {Image = Images.Person, Accessory = UITableViewCellAccessory.DisclosureIndicator };
            _assigneeElement.Tapped += () => ViewModel.GoToAssigneeCommand.Execute(null);

            _labelsElement = new StyledStringElement("Labels", "None", UITableViewCellStyle.Value1) {Image = Images.Tag, Accessory = UITableViewCellAccessory.DisclosureIndicator};
            _labelsElement.Tapped += () => ViewModel.GoToLabelsCommand.Execute(null);

            _addCommentElement = new StyledStringElement("Add Comment") { Image = Images.Pencil };
            _addCommentElement.Tapped += AddCommentTapped;

            NavigationItem.RightBarButtonItem = new UIBarButtonItem(UIBarButtonSystemItem.Action, (s, e) => ShowExtraMenu());
            NavigationItem.RightBarButtonItem.Enabled = false;
            ViewModel.Bind(x => x.IsLoading, x => 
            {
                if (!x)
                {
                    NavigationItem.RightBarButtonItem.Enabled = ViewModel.Issue != null;
                }
            });

            ViewModel.Bind(x => x.IsModifying, x =>
            {
                if (x)
                    _hud.Show("Loading...");
                else
                    _hud.Hide();
            });

            ViewModel.Bind(x => x.Issue, x =>
            {
                _assigneeElement.Value = x.Assignee != null ? x.Assignee.Login : "Unassigned".t();
                _milestoneElement.Value = x.Milestone != null ? x.Milestone.Title : "No Milestone";
                _labelsElement.Value = x.Labels.Count == 0 ? "None" : string.Join(", ", x.Labels.Select(i => i.Name));
                _descriptionElement.Value = ViewModel.MarkdownDescription;
                _header.Title = x.Title;
                _header.Subtitle = "Updated " + x.UpdatedAt.ToDaysAgo();
                Render();
            });

            ViewModel.BindCollection(x => x.Comments, (e) => RenderComments());
            ViewModel.BindCollection(x => x.Events, (e) => RenderComments());
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            Title = "Issue #" + ViewModel.Id;
        }

        private IEnumerable<CommentModel> CreateCommentList()
        {
            var items = ViewModel.Comments.Select(x => new CommentModel 
            { 
                AvatarUrl = x.User.AvatarUrl, 
                Login = x.User.Login, 
                CreatedAt = x.CreatedAt,
                Body = ViewModel.ConvertToMarkdown(x.Body)
            })
                .Concat(ViewModel.Events.Select(x => new CommentModel
            {
                AvatarUrl = x.Actor.AvatarUrl, 
                Login = x.Actor.Login, 
                CreatedAt = x.CreatedAt,
                Body = CreateEventBody(x.Event, x.CommitId)
            })
                .Where(x => !string.IsNullOrEmpty(x.Body)));

            return items.OrderBy(x => x.CreatedAt);
        }

        private static string CreateEventBody(string eventType, string commitId)
        {
            commitId = commitId ?? string.Empty;
            var smallCommit = commitId;
            if (string.IsNullOrEmpty(smallCommit))
                smallCommit = "Unknown";
            else if (smallCommit.Length > 7)
                smallCommit = commitId.Substring(0, 7);

            if (eventType == "closed")
                return "<p><span class=\"label label-danger\">Closed</span> this issue.</p>";
            if (eventType == "reopened")
                return "<p><span class=\"label label-success\">Reopened</span> this issue.</p>";
            if (eventType == "merged")
                return "<p><span class=\"label label-info\">Merged</span> commit " + smallCommit + "</p>";
            if (eventType == "referenced")
                return "<p><span class=\"label label-default\">Referenced</span> commit " + smallCommit + "</p>";
            return string.Empty;
        }

        public void RenderComments()
        {
            var s = Cirrious.CrossCore.Mvx.Resolve<CodeFramework.Core.Services.IJsonSerializationService>();
            var comments = CreateCommentList().Select(x => new {
                avatarUrl = x.AvatarUrl,
                login = x.Login,
                created_at = x.CreatedAt.ToDaysAgo(),
                body = x.Body
            });
            var data = s.Serialize(comments);

            InvokeOnMainThread(() => {
                _commentsElement.Value = !comments.Any() ? string.Empty : data;
                if (_commentsElement.GetImmediateRootElement() == null)
                    Render();
            });
        }

        protected virtual void Render()
        {
            //Wait for the issue to load
            if (ViewModel.Issue == null)
                return;

            var root = new RootElement(Title);
            root.Add(new Section(_header));

            var secDetails = new Section();
            if (!string.IsNullOrEmpty(_descriptionElement.Value))
                secDetails.Add(_descriptionElement);

            secDetails.Add(_assigneeElement);
            secDetails.Add(_milestoneElement);
            secDetails.Add(_labelsElement);
            root.Add(secDetails);

            if (!string.IsNullOrEmpty(_commentsElement.Value))
                root.Add(new Section { _commentsElement });

            root.Add(new Section { _addCommentElement });
            Root = root;
        }

        void AddCommentTapped()
        {
            var composer = new MarkdownComposerViewController();
            composer.NewComment(this, async (text) => {

                var hud = this.CreateHud();
                hud.Show("Posting Comment...");
                if (await ViewModel.AddComment(text))
                    composer.CloseComposer();
                hud.Hide();
                composer.EnableSendButton = true;
            });
        }

        public override UIView InputAccessoryView
        {
            get
            {
                var u = new UIView(new System.Drawing.RectangleF(0, 0, 320f, 27)) { BackgroundColor = UIColor.White };
                return u;
            }
        }


        private void ShowExtraMenu()
        {
            if (ViewModel.Issue == null)
                return;

            var sheet = MonoTouch.Utilities.GetSheet(Title);
            var editButton = sheet.AddButton("Edit".t());
            var openButton = sheet.AddButton(ViewModel.Issue.State == "open" ? "Close".t() : "Open".t());
            var commentButton = sheet.AddButton("Comment".t());
            var shareButton = sheet.AddButton("Share".t());
            var showButton = sheet.AddButton("Show in GitHub".t());
            var cancelButton = sheet.AddButton("Cancel".t());
            sheet.CancelButtonIndex = cancelButton;
            sheet.DismissWithClickedButtonIndex(cancelButton, true);
            sheet.Clicked += (s, e) => {
                if (e.ButtonIndex == editButton)
                    ViewModel.GoToEditCommand.Execute(null);
                else if (e.ButtonIndex == openButton)
                    ViewModel.ToggleStateCommand.Execute(null);
                else if (e.ButtonIndex == shareButton)
                    ViewModel.ShareCommand.Execute(ViewModel.Issue.HtmlUrl);
                else if (e.ButtonIndex == showButton)
                    ViewModel.GoToUrlCommand.Execute(ViewModel.Issue.HtmlUrl);
                else if (e.ButtonIndex == commentButton)
                    AddCommentTapped();
            };

            sheet.ShowInView(this.View);
        }

        private class CommentModel
        {
            public string AvatarUrl { get; set; }
            public string Login { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public string Body { get; set; }
        }
    }
}


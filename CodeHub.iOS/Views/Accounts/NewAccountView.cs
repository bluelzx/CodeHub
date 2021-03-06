using System.Drawing;
using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Touch.Views;
using CodeHub.Core.ViewModels.Accounts;
using MonoTouch;
using MonoTouch.UIKit;
using Cirrious.CrossCore;
using CodeHub.Core.Services;
using CodeHub.iOS.Views.App;

namespace CodeHub.iOS.Views.Accounts
{
    public partial class NewAccountView : MvxViewController
    {
        public NewAccountView()
			: base ("NewAccountView", null)
        {
            Title = "Account";
			NavigationItem.LeftBarButtonItem = new UIBarButtonItem(Theme.CurrentTheme.BackButton, UIBarButtonItemStyle.Plain,(s, e) => NavigationController.PopViewControllerAnimated(true));
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

			View.BackgroundColor = UIColor.FromRGB(239, 239, 244);
			
            InternetButton.SetBackgroundImage(Images.Buttons.GreyButton.CreateResizableImage(new UIEdgeInsets(18, 18, 18, 18)), UIControlState.Normal);
            EnterpriseButton.SetBackgroundImage(Images.Buttons.BlackButton.CreateResizableImage(new UIEdgeInsets(18, 18, 18, 18)), UIControlState.Normal);

            InternetButton.Layer.ShadowColor = UIColor.Black.CGColor;
            InternetButton.Layer.ShadowOffset = new SizeF(0, 1);
            InternetButton.Layer.ShadowOpacity = 0.3f;

            EnterpriseButton.Layer.ShadowColor = UIColor.Black.CGColor;
            EnterpriseButton.Layer.ShadowOffset = new SizeF(0, 1);
            EnterpriseButton.Layer.ShadowOpacity = 0.3f;

            var set = this.CreateBindingSet<NewAccountView, NewAccountViewModel>();
            set.Bind(InternetButton).To(x => x.GoToDotComLoginCommand);
            set.Apply();

            EnterpriseButton.TouchUpInside += (object sender, System.EventArgs e) => GoToEnterprise();

            ScrollView.ContentSize = new SizeF(View.Bounds.Width, EnterpriseButton.Frame.Bottom + 10f);
        }

        private void GoToEnterprise()
        {
            var features = Mvx.Resolve<IFeaturesService>();
            if (features.IsEnterpriseSupportActivated)
                ((NewAccountViewModel)ViewModel).GoToEnterpriseLoginCommand.Execute(null);
            else
            {
                var ctrl = new EnableEnterpriseViewController();
                ctrl.Dismissed += (sender, e) => {
                    if (features.IsEnterpriseSupportActivated)
                        ((NewAccountViewModel)ViewModel).GoToEnterpriseLoginCommand.Execute(null);
                };
                PresentViewController(ctrl, true, null);
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Drawing;

using System.Threading.Tasks;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreAnimation;

namespace XamarinStore.iOS
{
	public class ProductDetailViewController : UITableViewController
	{
		public event Action<Product> AddToBasket = delegate {};

		Product CurrentProduct;

		ProductSize[] sizeOptions;
		BottomButtonView BottomView;
		ProductColor[] colorOptions;
		StringSelectionCell colorCell, sizeCell;
		JBKenBurnsView imageView;
		UIImage tshirtIcon;
		public ProductDetailViewController (Product product)
		{
			CurrentProduct = product;

			Title = CurrentProduct.Name;
			LoadProductData ();
			TableView.TableFooterView = new UIView (new RectangleF (0, 0, 0, BottomButtonView.Height));

			View.AddSubview (BottomView = new BottomButtonView () {
				ButtonText = "Add to Basket",
				Button = {
					Image = (tshirtIcon = UIImage.FromBundle("t-shirt")),
				},
				ButtonTapped = async () => await addToBasket ()
			});

		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();
			AppDelegate.Shared.AddToBasketComplete += async (result) => await HandleAddToBasketComplete(result);
		}
			
		private async Task HandleAddToBasketComplete(bool result)
		{
			if (!result && NavigationController != null) {
				await RemoveFromBasket ();
				(new UIAlertView("Don't be greedy!", "We currently only allow one free t-shirt per person.", 
					null,"Ok, sorry...")).Show();
			}

			BottomView.Button.Enabled = true;
		}

		private async Task SetupBasketAnimation(bool reverse = false)
		{
			BottomView.Button.Enabled = false;
			var center = BottomView.Button.ConvertPointToView (BottomView.Button.ImageView.Center, NavigationController.View);
			var tshirtImageView = new UIImageView (tshirtIcon) { 
				Center = center,
				ContentMode = UIViewContentMode.ScaleAspectFill
			};
			var circle = (reverse) ? "redcircle" : "circle";
			var backgroundView = new UIImageView (UIImage.FromBundle(circle)) {
				Center = center,
			};
			NavigationController.View.AddSubview (backgroundView);
			NavigationController.View.AddSubview (tshirtImageView);
			await Task.WhenAll (new [] {
				animateView (tshirtImageView, reverse),
				animateView (backgroundView, reverse),
			});

			NavigationItem.RightBarButtonItem = AppDelegate.Shared.CreateBasketButton ();
		}

		private async Task addToBasket()
		{
			await SetupBasketAnimation ();
			AddToBasket (CurrentProduct);
		}

		async Task RemoveFromBasket()
		{
			await SetupBasketAnimation (true);
		}


		async Task animateView(UIView view, bool reverse = false)
		{
			var size = view.Frame.Size;
			var grow = new SizeF(size.Width * 1.7f, size.Height * 1.7f);
			var shrink = new SizeF(size.Width * .4f, size.Height * .4f);

			var endPoint = new PointF (290, 34);
			var controlPoint = new PointF (view.Center.X, View.Center.Y);

			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
			//Set the animation path
			var pathAnimation = CAKeyFrameAnimation.GetFromKeyPath("position");	
			pathAnimation.CalculationMode = CAAnimation.AnimationPaced;
			pathAnimation.FillMode = CAFillMode.Forwards;
			pathAnimation.RemovedOnCompletion = false;
			pathAnimation.Duration = .5;

			UIBezierPath path = new UIBezierPath ();

			if (reverse) {
				path.MoveTo (endPoint);
				path.AddQuadCurveToPoint (view.Center, controlPoint);
			}
			else {
				path.MoveTo (view.Center);
				path.AddQuadCurveToPoint (endPoint, controlPoint);
			}
				
			pathAnimation.Path = path.CGPath;

			//Set size change
			CABasicAnimation growAnimation;
			CABasicAnimation shrinkAnimation;
			if (reverse) {
				growAnimation = CABasicAnimation.FromKeyPath ("bounds.size");
				growAnimation.From = NSValue.FromSizeF (shrink);
				growAnimation.To = NSValue.FromSizeF (grow);
				growAnimation.FillMode = CAFillMode.Forwards;
				growAnimation.Duration = .4;
				growAnimation.RemovedOnCompletion = false;

				shrinkAnimation = CABasicAnimation.FromKeyPath ("bounds.size");
				shrinkAnimation.To = NSValue.FromSizeF (shrink);
				shrinkAnimation.FillMode = CAFillMode.Forwards;
				shrinkAnimation.Duration = .1;
				shrinkAnimation.RemovedOnCompletion = false;
				shrinkAnimation.BeginTime = .4;
			}
			else {
				growAnimation = CABasicAnimation.FromKeyPath("bounds.size");
				growAnimation.To = NSValue.FromSizeF (grow);
				growAnimation.FillMode = CAFillMode.Forwards;
				growAnimation.Duration = .1;
				growAnimation.RemovedOnCompletion = false;

				shrinkAnimation = CABasicAnimation.FromKeyPath("bounds.size");
				shrinkAnimation.From = NSValue.FromSizeF (grow);
				shrinkAnimation.To = NSValue.FromSizeF (shrink);
				shrinkAnimation.FillMode = CAFillMode.Forwards;
				shrinkAnimation.Duration = .4;
				shrinkAnimation.RemovedOnCompletion = false;
				shrinkAnimation.BeginTime = .1;
			}

			CAAnimationGroup animations = new CAAnimationGroup ();
			animations.FillMode = CAFillMode.Forwards;
			animations.RemovedOnCompletion = false;
			animations.Animations = new CAAnimation[] {
				pathAnimation,
				growAnimation,
				shrinkAnimation,
			};

			animations.Duration = .5;
			animations.AnimationStopped += (sender, e) => {
				tcs.TrySetResult(true);
			};
			view.Layer.AddAnimation (animations,"movetocart");
			NSTimer.CreateScheduledTimer (.5, () => view.RemoveFromSuperview ());
			await tcs.Task;

		}

		string[] imageUrls = new string[0];
		public void LoadProductData ()
		{
			// Add spinner while loading data.
			TableView.Source = new ProductDetailPageSource (new [] {
				new SpinnerCell(),
			});

			colorOptions = CurrentProduct.Colors;
			sizeOptions = CurrentProduct.Sizes;
			imageUrls =  CurrentProduct.ImageUrls.ToArray().Shuffle();

			imageView = new JBKenBurnsView {
				Frame = new RectangleF (0, -60, 320, 400),
				Images = Enumerable.Range(0,imageUrls.Length).Select(x=> new UIImage()).ToList(),
				UserInteractionEnabled = false,
			};
			loadImages ();
			var productDescriptionView = new ProductDescriptionView (CurrentProduct) {
				Frame = new RectangleF (0, 0, 320, 120),
			};
			TableView.TableHeaderView = new UIView(new RectangleF(0,0,imageView.Frame.Width,imageView.Frame.Bottom)){imageView};
			var tableItems = new List<UITableViewCell> () {
				new CustomViewCell (productDescriptionView),
			};
			tableItems.AddRange (GetOptionsCells ());

			TableView.Source = new ProductDetailPageSource (tableItems.ToArray ());
			TableView.ReloadData ();
		}


		async void loadImages()
		{
			for (int i = 0; i < imageUrls.Length; i++) {
				var path = await FileCache.Download (Product.ImageForSize (imageUrls [i], 320 * UIScreen.MainScreen.Scale));
				imageView.Images [i] = UIImage.FromFile (path);
			}
		}


		IEnumerable<UITableViewCell> GetOptionsCells ()
		{
			yield return sizeCell = new StringSelectionCell (View) {
				Text = "Size",
				Items = sizeOptions.Select (x => x.Description),
				DetailText = CurrentProduct.Size.Description,
				SelectionChanged = () => {
					var size = sizeOptions [sizeCell.SelectedIndex];
					CurrentProduct.Size = size;
				}
			};

			yield return colorCell = new StringSelectionCell (View) {
				Text = "Color",
				Items = colorOptions.Select (x => x.Name),
				DetailText = CurrentProduct.Color.Name,
				SelectionChanged = () => {
					var color = colorOptions [colorCell.SelectedIndex];
					CurrentProduct.Color = color;
				},
			};
		}

		public override void ViewWillAppear (bool animated)
		{
			base.ViewWillAppear (animated);
			NavigationItem.RightBarButtonItem = AppDelegate.Shared.CreateBasketButton ();
			imageView.Animate();
			var bottomRow = NSIndexPath.FromRowSection (TableView.NumberOfRowsInSection (0) - 1, 0);
			TableView.ScrollToRow (bottomRow,UITableViewScrollPosition.Top, false);
		}

		public override void ViewDidLayoutSubviews ()
		{
			base.ViewDidLayoutSubviews ();

			var bound = View.Bounds;
			bound.Y = bound.Bottom - BottomButtonView.Height;
			bound.Height = BottomButtonView.Height;
			BottomView.Frame = bound;
		}
	}

	public class ProductDetailPageSource : UITableViewSource
	{
		UITableViewCell[] tableItems;

		public ProductDetailPageSource (UITableViewCell[] items)
		{
			tableItems = items;
		}

		public override int RowsInSection (UITableView tableview, int section)
		{
			return tableItems.Length;
		}

		public override UITableViewCell GetCell (UITableView tableView, MonoTouch.Foundation.NSIndexPath indexPath)
		{
			return tableItems [indexPath.Row];
		}

		public override float GetHeightForRow (UITableView tableView, NSIndexPath indexPath)
		{
			return tableItems [indexPath.Row].Frame.Height;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath)
		{
			if (tableItems [indexPath.Row] is StringSelectionCell)
				((StringSelectionCell)tableItems [indexPath.Row]).Tap ();

			tableView.DeselectRow (indexPath, true);
		}
		
	}
}
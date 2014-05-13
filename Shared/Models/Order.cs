using System;
using System.Collections.Generic;
using System.Linq;

namespace XamarinStore
{
	public class Order
	{
		public event EventHandler ProductsChanged;
		List<Product> products = new List<Product> ();

		public IReadOnlyList<Product> Products { 
			get {
				return products;
			}
			set{
				products = value.ToList();
			}
		}

		public bool CanAdd(Product product)
		{
			// We could do more things here, like check availability of the requested product
			// For now, just check that we don't have a product yet (only one tshirt per order)
			return (products.Count == 0);
		}

		public bool Add (Product product)
		{
			if (!CanAdd (product))
				return false;

			products.Insert (0,(Product)product.Clone());
			var evt = ProductsChanged;
			if (evt != null)
				evt (this, EventArgs.Empty);

			return true;
		}

		public bool Remove (Product product)
		{
			var result = products.Remove (product);
			if (result) {
				var evt = ProductsChanged;
				if (evt != null)
					evt (this, EventArgs.Empty);
			}
			return result;
		}

		public string SsoToken { get; set; }
		public string ShippingName { get; set; }
		public string Email { get; set; }
		public string Address1 { get; set; }
		public string Address2 { get; set; }
		public string City { get; set; }
		public string State { get; set; }
		public string ZipCode { get; set; }
		public string Phone { get; set; }
		public string Country { get; set; }

		public string GetJson(User user)
		{
			ShippingName = string.Format ("{0} {1}",user.FirstName, user.LastName);
			SsoToken = user.Token;
			Email = user.Email;
			Address1 = user.Address;
			Address2 = user.Address2;
			City = user.City;
			State = user.State;
			ZipCode = user.ZipCode;
			Phone = user.Phone;
			Country = user.Country;

			return Newtonsoft.Json.JsonConvert.SerializeObject (this);
		}
	}
}


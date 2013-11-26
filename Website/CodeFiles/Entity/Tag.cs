using Newtonsoft.Json;

namespace GalleryServerPro.Web.Entity
{
	/// <summary>
	/// A client-optimized object representing a tag or person.
	/// </summary>
	public class Tag
	{
		/// <summary>
		/// Gets or sets the value of the tag or person.
		/// </summary>
		/// <value>The value.</value>
		[JsonProperty(PropertyName = "value")]
		public string Value { get; set; }
	}
}
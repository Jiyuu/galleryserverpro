using GalleryServerPro.Business.Interfaces;

namespace GalleryServerPro.Business
{
	/// <summary>
	/// An object that specifies options for retrieving gallery objects. Used in conjunction with the
	/// <see cref="TagSearcher" /> class.
	/// </summary>
	public class TagSearchOptions
	{
		/// <summary>
		/// Specifies the type of tag search.
		/// </summary>
		public TagSearchType SearchType;

		/// <summary>
		/// The gallery ID. Only items in this gallery are returned.
		/// </summary>
		public int GalleryId;

		public string SearchTerm;

		/// <summary>
		/// The roles the current user belongs to. Required when <see cref="IsUserAuthenticated" />=<c>true</c>; 
		/// otherwise, the value can be left null.
		/// </summary>
		public IGalleryServerRoleCollection Roles;

		/// <summary>
		/// Indicates whether the current user has been authenticated.
		/// </summary>
		public bool IsUserAuthenticated;
	}
}

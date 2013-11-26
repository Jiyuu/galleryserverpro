using GalleryServerPro.Business.Interfaces;
using GalleryServerPro.Business.Metadata;
using GalleryServerPro.Business.Properties;
using GalleryServerPro.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GalleryServerPro.Business
{
	/// <summary>
	/// Provides functionality for finding one or more gallery objects.
	/// </summary>
	public class GalleryObjectSearcher
	{
		#region Fields

		private IAlbum _rootAlbum;
		private bool? _userCanViewRootAlbum;

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the search options.
		/// </summary>
		/// <value>The search options.</value>
		private GalleryObjectSearchOptions SearchOptions { get; set; }

		/// <summary>
		/// Gets the type of the tag to search for. Applies only when the search type is <see cref="GalleryObjectSearchType.SearchByTag" />
		/// or <see cref="GalleryObjectSearchType.SearchByPeople" />.
		/// </summary>
		/// <value>The type of the tag.</value>
		private MetadataItemName TagType
		{
			get
			{
				return (SearchOptions.SearchType == GalleryObjectSearchType.SearchByTag ? MetadataItemName.Tags : MetadataItemName.People);
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current user can view the root album.
		/// </summary>
		/// <returns><c>true</c> if the user can view the root album; otherwise, <c>false</c>.</returns>
		private bool UserCanViewRootAlbum
		{
			get
			{
				if (!_userCanViewRootAlbum.HasValue)
				{
					_userCanViewRootAlbum = HelperFunctions.CanUserViewAlbum(RootAlbum, SearchOptions.Roles, SearchOptions.IsUserAuthenticated);
				}

				return _userCanViewRootAlbum.Value;
			}
		}

		/// <summary>
		/// Gets the root album for the gallery identified in the <see cref="SearchOptions" />.
		/// </summary>
		private IAlbum RootAlbum
		{
			get { return _rootAlbum ?? (_rootAlbum = Factory.LoadRootAlbumInstance(SearchOptions.GalleryId)); }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="GalleryObjectSearcher" /> class.
		/// </summary>
		/// <param name="searchOptions">The search options.</param>
		/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="searchOptions" /> is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown when one or more properties of the <paramref name="searchOptions" /> parameter is invalid.</exception>
		public GalleryObjectSearcher(GalleryObjectSearchOptions searchOptions)
		{
			Validate(searchOptions);

			SearchOptions = searchOptions;

			if (SearchOptions.Roles == null)
			{
				SearchOptions.Roles = new GalleryServerRoleCollection();
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Finds the first gallery object that matches the criteria. Use this method when a single item is expected.
		/// Returns null when no matching items are found.
		/// </summary>
		/// <returns>An instance of <see cref="IGalleryObject" /> or null.</returns>
		public IGalleryObject FindOne()
		{
			return Find().FirstOrDefault();
		}

		/// <summary>
		/// Finds all gallery objects that match the search criteria. Guaranteed to not return null.
		/// </summary>
		/// <returns>IGalleryObjectCollection.</returns>
		/// <exception cref="System.InvalidOperationException">Thrown when an implementation is not found for one of the 
		/// search types.</exception>
		public IEnumerable<IGalleryObject> Find()
		{
			switch (SearchOptions.SearchType)
			{
				case GalleryObjectSearchType.SearchByTitleOrCaption:
					return FindItemsMatchingTitleOrCaption();
				case GalleryObjectSearchType.SearchByKeyword:
					return FindItemsMatchingKeywords();
				case GalleryObjectSearchType.SearchByTag:
				case GalleryObjectSearchType.SearchByPeople:
					return FindItemsMatchingTags();
				case GalleryObjectSearchType.HighestAlbumUserCanView:
					return WrapInGalleryObjectCollection(LoadRootAlbumForUser());
				default:
					throw new InvalidOperationException(string.Format("The method GalleryObjectSearcher.Find was not designed to handle SearchType={0}. The developer must update this method.", SearchOptions.SearchType));
			}
		}

		private IEnumerable<IGalleryObject> FindItemsMatchingTitleOrCaption()
		{
			var results = new GalleryObjectCollection();

			var metaTagsToSearch = new[] { MetadataItemName.Title, MetadataItemName.Caption };

			using (var repo = new AlbumRepository())
			{
				var qry = repo.Where(a => true, a => a.Metadata);

				qry = SearchOptions.SearchTerms.Aggregate(qry, (current, searchTerm) => current.Where(a => 
					a.FKGalleryId == SearchOptions.GalleryId && 
					a.Metadata.Any(md => metaTagsToSearch.Contains(md.MetaName) && md.Value.Contains(searchTerm))));

				foreach (var album in qry)
				{
					results.Add(Factory.GetAlbumFromDto(album));
				}
			}

			using (var repo = new MediaObjectRepository())
			{
				var qry = repo.Where(a => true, a => a.Metadata);

				qry = SearchOptions.SearchTerms.Aggregate(qry, (current, searchTerm) => current.Where(mo => 
					mo.Album.FKGalleryId == SearchOptions.GalleryId && 
					mo.Metadata.Any(md => metaTagsToSearch.Contains(md.MetaName) && md.Value.Contains(searchTerm))));

				foreach (var mediaObject in qry)
				{
					results.Add(Factory.GetMediaObjectFromDto(mediaObject, null));
				}
			}

			return results;
		}

		private IEnumerable<IGalleryObject> FindItemsMatchingKeywords()
		{
			var results = new GalleryObjectCollection();

			using (var repo = new AlbumRepository())
			{
				var qry = repo.Where(a => true, a => a.Metadata);

				qry = SearchOptions.SearchTerms.Aggregate(qry, (current, searchTerm) => current.Where(a => 
					a.FKGalleryId == SearchOptions.GalleryId &&
					a.Metadata.Any(md => md.Value.Contains(searchTerm))));

				foreach (var album in qry)
				{
					results.Add(Factory.GetAlbumFromDto(album));
				}
			}

			using (var repo = new MediaObjectRepository())
			{
				var qry = repo.Where(a => true, a => a.Metadata);

				qry = SearchOptions.SearchTerms.Aggregate(qry, (current, searchTerm) => current.Where(mo => 
					mo.Album.FKGalleryId == SearchOptions.GalleryId && 
					mo.Metadata.Any(md => md.Value.Contains(searchTerm))));

				foreach (var mediaObject in qry)
				{
					results.Add(Factory.GetMediaObjectFromDto(mediaObject, null));
				}
			}

			return results;
		}

		#endregion

		#region Functions

		/// <summary>
		/// Validates the specified search options. Throws an exception if not valid.
		/// </summary>
		/// <param name="searchOptions">The search options.</param>
		/// <exception cref="System.ArgumentNullException">Thrown when <paramref name="searchOptions" /> is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown when one or more properties of the <paramref name="searchOptions" /> parameter is invalid.</exception>
		private static void Validate(GalleryObjectSearchOptions searchOptions)
		{
			if (searchOptions == null)
				throw new ArgumentNullException("searchOptions");

			if (searchOptions.SearchType == GalleryObjectSearchType.NotSpecified)
				throw new ArgumentException("The SearchType property of the searchOptions parameter must be set to a valid search type.");

			if (searchOptions.IsUserAuthenticated && searchOptions.Roles == null)
				throw new ArgumentException("The Roles property of the searchOptions parameter must be specified when IsUserAuthenticated is true.");

			if (searchOptions.GalleryId < 0) // v3+ galleries start at 1, but galleries from earlier versions begin at 0
				throw new ArgumentException("Invalid gallery ID. The GalleryId property of the searchOptions parameter must refer to a valid gallery.");

			if ((searchOptions.SearchType == GalleryObjectSearchType.SearchByTag || searchOptions.SearchType == GalleryObjectSearchType.SearchByPeople) && (searchOptions.Tags == null || searchOptions.Tags.Length == 0))
				throw new ArgumentException("The Tags property of the searchOptions parameter must be specified when SearchType is SearchByTag or SearchByPeople.");
		}

		/// <summary>
		/// Finds the gallery objects matching tags. Guaranteed to not return null. Call this function only when the search type
		/// is <see cref="GalleryObjectSearchType.SearchByTag" /> or <see cref="GalleryObjectSearchType.SearchByPeople" />.
		/// </summary>
		/// <returns>An instance of <see cref="IGalleryObjectCollection" />.</returns>
		private IEnumerable<IGalleryObject> FindItemsMatchingTags()
		{
			var galleryObjects = new GalleryObjectCollection();

			galleryObjects.AddRange(GetAlbumsHavingTags());

			galleryObjects.AddRange(GetMediaObjectsHavingTags());

			return galleryObjects;
		}

		/// <summary>
		/// Gets the albums having all tags specified in the search options. Guaranteed to not return null. Only albums the 
		/// user has permission to view are returned.
		/// </summary>
		/// <returns>An instance of <see cref="IGalleryObjectCollection" />.</returns>
		private IEnumerable<IGalleryObject> GetAlbumsHavingTags()
		{
			var galleryObjects = new GalleryObjectCollection();

			using (var repo = new AlbumRepository())
			{
				foreach (var albumId in GetAlbumsIdsHavingTags(repo))
				{
					var album = Factory.LoadAlbumInstance(albumId, false);
					// We have an album that contains at least one of the tags. If we have multiple tags, do an extra test to ensure
					// album matches ALL of them. (I wasn't able to write the LINQ to do this for me, so it's an extra step.)
					if (SearchOptions.Tags.Length == 1)
					{
						galleryObjects.Add(album);
					}
					else if (MetadataItemContainsAllTags(album.MetadataItems.First(md => md.MetadataItemName == TagType)))
					{
						galleryObjects.Add(album);
					}
				}
			}

			return galleryObjects;
		}

		/// <summary>
		/// Gets the albums IDs having all tags specified in the search options. Guarantees that only albums the user has permission
		/// to view are returned.
		/// </summary>
		/// <param name="repo">The album repository.</param>
		/// <returns>An instance of IEnumerable&lt;System.Int32&gt;.</returns>
		/// <remarks>This function is similar to <see cref="GetMediaObjectIdsHavingTags(IRepository{MediaObjectDto})" />, so if a developer
		/// modifies it, be sure to check that function to see if it needs a similar change.</remarks>
		private IEnumerable<int> GetAlbumsIdsHavingTags(IRepository<AlbumDto> repo)
		{
			var qry = repo.Where(
				a =>
				a.FKGalleryId == SearchOptions.GalleryId &&
				a.Metadata.Any(md => md.MetaName == TagType && md.MetadataTags.Any(mdt => SearchOptions.Tags.Contains(mdt.FKTagName))));

			if (SearchOptions.IsUserAuthenticated)
			{
				var rootAlbum = Factory.LoadRootAlbumInstance(SearchOptions.GalleryId);

				if (!CanUserViewAlbum(rootAlbum))
				{
					// User can't view the root album, so get a list of the albums she *can* see and make sure our 
					// results only include albums that are viewable.
					var albumIds = SearchOptions.Roles.GetViewableAlbumIdsForGallery(SearchOptions.GalleryId);

					qry = qry.Where(a => albumIds.Contains(a.AlbumId));
				}
			}
			else
			{
				// Anonymous user, so don't include any private albums in results.
				qry = qry.Where(a => !a.IsPrivate);
			}

			return qry.Select(a => a.AlbumId);
		}

		/// <summary>
		/// Determines whether the current user can view the specified <paramref name="album" />.
		/// </summary>
		/// <param name="album">The album.</param>
		/// <returns><c>true</c> if the user can view the album; otherwise, <c>false</c>.</returns>
		private bool CanUserViewAlbum(IAlbum album)
		{
			return SecurityManager.IsUserAuthorized(SecurityActions.ViewAlbumOrMediaObject, SearchOptions.Roles, album.Id, SearchOptions.GalleryId, SearchOptions.IsUserAuthenticated, album.IsPrivate, SecurityActionsOption.RequireOne, album.IsVirtualAlbum);
		}

		/// <summary>
		/// Returns a value indicating whether the <paramref name="mdItem" /> contains ALL the tags contained in SearchOptions.Tags.
		/// The comparison is case insensitive.
		/// </summary>
		/// <param name="mdItem">The metadata item.</param>
		/// <returns><c>true</c> if the metadata item contains all the tags, <c>false</c> otherwise</returns>
		private bool MetadataItemContainsAllTags(IGalleryObjectMetadataItem mdItem)
		{
			// First split the meta value into the separate tag items, trimming and converting to lower case.
			var albumTags = mdItem.Value.ToLowerInvariant().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());

			// Now make sure that albumTags contains ALL the items in SearchOptions.Tags.
			return SearchOptions.Tags.Aggregate(true, (current, tag) => current & albumTags.Contains(tag.ToLowerInvariant()));
		}

		/// <summary>
		/// Gets the media objects having all tags specified in the search options. Guaranteed to not return null. Only media objects the 
		/// user has permission to view are returned.
		/// </summary>
		/// <returns>An instance of <see cref="IGalleryObjectCollection" />.</returns>
		private IEnumerable<IGalleryObject> GetMediaObjectsHavingTags()
		{
			var galleryObjects = new GalleryObjectCollection();

			using (var repo = new MediaObjectRepository())
			{
				foreach (var mediaObjectId in GetMediaObjectIdsHavingTags(repo))
				{
					var mediaObject = Factory.LoadMediaObjectInstance(mediaObjectId);
					// We have a media object that contains at least one of the tags. If we have multiple tags, do an extra test to ensure
					// media object matches ALL of them. (I wasn't able to write the LINQ to do this for me, so it's an extra step.)
					if (SearchOptions.Tags.Length == 1)
					{
						galleryObjects.Add(mediaObject);
					}
					else if (MetadataItemContainsAllTags(mediaObject.MetadataItems.First(md => md.MetadataItemName == TagType)))
					{
						galleryObjects.Add(mediaObject);
					}
				}
			}

			return galleryObjects;
		}

		/// <summary>
		/// Gets the media object IDs having all tags specified in the search options. Guarantees that only media objects the 
		/// user has permission to view are returned.
		/// </summary>
		/// <param name="repo">The media object repository.</param>
		/// <returns>An instance of IEnumerable{System.Int32}.</returns>
		/// <remarks>This function is similar to <see cref="GetAlbumsIdsHavingTags(IRepository{AlbumDto})" />, so if a developer
		/// modifies it, be sure to check that function to see if it needs a similar change.</remarks>
		private IEnumerable<int> GetMediaObjectIdsHavingTags(IRepository<MediaObjectDto> repo)
		{
			var qry = repo.Where(
				m =>
				m.Album.FKGalleryId == SearchOptions.GalleryId &&
				m.Metadata.Any(md => md.MetaName == TagType && md.MetadataTags.Any(mdt => SearchOptions.Tags.Contains(mdt.FKTagName))));

			if (SearchOptions.IsUserAuthenticated)
			{
				var rootAlbum = Factory.LoadRootAlbumInstance(SearchOptions.GalleryId);

				if (!CanUserViewAlbum(rootAlbum))
				{
					// User can't view the root album, so get a list of the albums she *can* see and make sure our 
					// results only include media objects that are viewable.
					var albumIds = SearchOptions.Roles.GetViewableAlbumIdsForGallery(SearchOptions.GalleryId);

					qry = qry.Where(a => albumIds.Contains(a.Album.AlbumId));
				}
			}
			else
			{
				// Anonymous user, so don't include any private albums in results.
				qry = qry.Where(m => !m.Album.IsPrivate);
			}

			return qry.Select(m => m.MediaObjectId);
		}

		/// <summary>
		/// Gets the top level album the current user has permission to view. Returns null when the user does not 
		/// have permission to view any albums.
		/// </summary>
		/// <returns>An instance of <see cref="IAlbum" /> or null.</returns>
		private IAlbum LoadRootAlbumForUser()
		{
			// Get list of root album IDs with view permission.

			// Step 1: Compile a list of album IDs having the requested permissions.
			var rootAlbums = GetRootAlbumsUserCanView();

			// Step 3: Package results into an album container. If there is only one viewable root album, then just create an instance of that album.
			// Otherwise, create a virtual root album to contain the multiple viewable albums.
			IAlbum rootAlbum;

			if (rootAlbums.Count == 0)
				return null;

			if (rootAlbums.Count == 1)
			{
				rootAlbum = rootAlbums[0];
			}
			else
			{
				// Create virtual album to serve as a container for the child albums the user has permission to view.
				rootAlbum = Factory.CreateEmptyAlbumInstance(SearchOptions.GalleryId);
				rootAlbum.IsVirtualAlbum = true;
				rootAlbum.VirtualAlbumType = VirtualAlbumType.Root;
				rootAlbum.Title = Resources.Virtual_Album_Title;
				rootAlbum.Caption = String.Empty;
				foreach (var album in rootAlbums)
				{
					rootAlbum.AddGalleryObject(album);
				}
			}

			return rootAlbum;
		}

		/// <summary>
		/// Gets a list of the top-level albums the current user can view. Guaranteed to not return null. Will be empty 
		/// if user does not have access to any albums.
		/// </summary>
		/// <returns>An instance of <see cref="List{IAlbum}" />.</returns>
		private List<IAlbum> GetRootAlbumsUserCanView()
		{
			// If user can view the root album, just return that.
			var rootAlbum = Factory.LoadRootAlbumInstance(SearchOptions.GalleryId);

			if (CanUserViewAlbum(rootAlbum))
			{
				return new List<IAlbum>() { rootAlbum };
			}
			else if (!SearchOptions.IsUserAuthenticated)
			{
				// Anonymous user can't view any albums, so just return an empty list.
				return new List<IAlbum>();
			}

			// Logged on user can't see root album, so calculate the top-level list of album IDs they *can* see.
			var allRootAlbumIds = SearchOptions.Roles.GetViewableAlbumIdsForGallery(SearchOptions.GalleryId);

			// Step 2: Convert previous list to contain ONLY top-level albums in the current gallery.
			var rootAlbums = RemoveChildAlbumsAndAlbumsInOtherGalleries(allRootAlbumIds);

			return rootAlbums;
		}

		/// <summary>
		/// Generate a new list containing a subset of <paramref name="allRootAlbumIds" /> that contains only a list of 
		/// top-level album IDs and albums belonging to the gallery specified in the search options.
		/// Any albums that have a parent - at any level - in the list are not included. Guaranteed to not return null.
		/// </summary>
		/// <param name="allRootAlbumIds">All album IDs to process.</param>
		/// <returns>Returns an enumerable list of integers representing the album IDs that satisfy the criteria.</returns>
		private List<IAlbum> RemoveChildAlbumsAndAlbumsInOtherGalleries(IEnumerable<int> allRootAlbumIds)
		{
			// Loop through our list of album IDs. If any album has an ancestor that is also in the list, then remove it. 
			// We only want a list of top level albums.
			var rootAlbums = new List<IAlbum>();
			var albumsToRemove = new List<IAlbum>();
			foreach (int viewableAlbumId in allRootAlbumIds)
			{
				var album = Factory.LoadAlbumInstance(viewableAlbumId, false);

				if (album.GalleryId != SearchOptions.GalleryId)
				{
					// The album belongs to a different gallery, so skip it. It won't get included in the returned collection.
					continue;
				}

				rootAlbums.Add(album);

				var albumParent = album;

				while (true)
				{
					albumParent = albumParent.Parent as IAlbum;
					if (albumParent == null)
						break;

					if (allRootAlbumIds.Contains(albumParent.Id))
					{
						albumsToRemove.Add(album);
						break;
					}
				}
			}
			foreach (var album in albumsToRemove)
			{
				rootAlbums.Remove(album);
			}

			return rootAlbums;
		}

		/// <summary>
		/// Wraps the <paramref name="album" /> in a gallery object collection. When <paramref name="album" /> is null,
		/// an empty collection is returned. Guaranteed to no return null. 
		/// </summary>
		/// <param name="album">The album.</param>
		/// <returns>An instance of <see cref="IGalleryObjectCollection" />.</returns>
		private static IEnumerable<IGalleryObject> WrapInGalleryObjectCollection(IAlbum album)
		{
			var result = new GalleryObjectCollection();

			if (album != null)
				result.Add(album);

			return result;
		}

		#endregion
	}
}

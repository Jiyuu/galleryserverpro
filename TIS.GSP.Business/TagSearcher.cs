﻿using GalleryServerPro.Business.Interfaces;
using GalleryServerPro.Business.Metadata;
using GalleryServerPro.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GalleryServerPro.Business
{
  /// <summary>
  /// Provides functionality for finding one or more descriptive tags or people.
  /// </summary>
  public class TagSearcher
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
    private TagSearchOptions SearchOptions { get; set; }

    /// <summary>
    /// Indicates the type of tag being searched. Must be <see cref="MetadataItemName.Tags" /> or 
    /// <see cref="MetadataItemName.People" />.
    /// </summary>
    private MetadataItemName TagName
    {
      get
      {
        switch (SearchOptions.SearchType)
        {
          case TagSearchType.TagsUserCanView:
          case TagSearchType.AllTagsInGallery:
            return MetadataItemName.Tags;

          case TagSearchType.PeopleUserCanView:
          case TagSearchType.AllPeopleInGallery:
            return MetadataItemName.People;

          default:
            throw new InvalidOperationException(String.Format("The property TagSearcher.TagName was not designed to handle the SearchType {0}. The developer must update this property.", SearchOptions.SearchType));
        }
      }
    }

    /// <summary>
    /// Gets the root album for the gallery identified in the <see cref="SearchOptions" />.
    /// </summary>
    private IAlbum RootAlbum
    {
      get { return _rootAlbum ?? (_rootAlbum = Factory.LoadRootAlbumInstance(SearchOptions.GalleryId)); }
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

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="TagSearcher" /> class.
    /// </summary>
    /// <param name="searchOptions">The search options.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="searchOptions" /> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when one or more properties of the <paramref name="searchOptions" /> parameter is invalid.</exception>
    public TagSearcher(TagSearchOptions searchOptions)
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
    /// Finds all tags that match the search criteria. Guaranteed to not return null.
    /// </summary>
    /// <returns>A collection of <see cref="TagDto" /> instances.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when an implementation is not found for one of the 
    /// search types.</exception>
    public IEnumerable<Entity.Tag> Find()
    {
      switch (SearchOptions.SearchType)
      {
        case TagSearchType.AllTagsInGallery:
        case TagSearchType.AllPeopleInGallery:
          return GetTags();
        case TagSearchType.TagsUserCanView:
        case TagSearchType.PeopleUserCanView:
          return GetTagsForUser();
        default:
          throw new InvalidOperationException(string.Format("The method GalleryObjectSearcher.Find was not designed to handle SearchType={0}. The developer must update this method.", SearchOptions.SearchType));
      }
    }

    #endregion

    #region Functions

    /// <summary>
    /// Validates the specified search options. Throws an exception if not valid.
    /// </summary>
    /// <param name="searchOptions">The search options.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="searchOptions" /> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when one or more properties of the <paramref name="searchOptions" /> parameter is invalid.</exception>
    private static void Validate(TagSearchOptions searchOptions)
    {
      if (searchOptions == null)
        throw new ArgumentNullException("searchOptions");

      if (searchOptions.SearchType == TagSearchType.NotSpecified)
        throw new ArgumentException("The SearchType property of the searchOptions parameter must be set to a valid search type.");

      if (searchOptions.IsUserAuthenticated && searchOptions.Roles == null)
        throw new ArgumentException("The Roles property of the searchOptions parameter must be specified when IsUserAuthenticated is true.");

      if (searchOptions.GalleryId < 0) // v3+ galleries start at 1, but galleries from earlier versions begin at 0
        throw new ArgumentException("Invalid gallery ID. The GalleryId property of the searchOptions parameter must refer to a valid gallery.");
    }

    /// <summary>
    /// Gets a list of tags the user can view. Guaranteed to not return null. Returns an empty collection
    /// when no tags are found.
    /// </summary>
    /// <returns>A collection of <see cref="Entity.Tag" /> instances.</returns>
    private IEnumerable<Entity.Tag> GetTagsForUser()
    {
      var tags = new List<Entity.Tag>();
      var hasMediaObjectTags = false;

      using (var repo = new MetadataTagRepository())
      {
        tags.AddRange(GetTagsForAlbums(repo));

        if (!UserCanViewRootAlbum)
        {
          // When the user can view the entire gallery, the GetTagsForAlbums() function returned all the tags, so we don't
          // need to look specifically at the tags belonging to media objects. But for restricted users we need this step.
          var tagsForMediaObjects = GetTagsForMediaObjects(repo);
          hasMediaObjectTags = tagsForMediaObjects.Any();

          tags.AddRange(tagsForMediaObjects);
        }
      }

      // Optimization: When there are media object tags, we need to combine the album and media object tags; otherwise we can
      // just return the tags.
      if (hasMediaObjectTags)
      {
        return SortAndFilter(tags.GroupBy(t => t.Value).Select(t => new Entity.Tag {Value = t.Key, Count = t.Sum(t1 => t1.Count)}));
      }
      else
      {
        return SortAndFilter(tags);
      }
    }

    /// <summary>
    /// Gets the tags associated with albums the user has permission to view. When the user has permission to view the
    /// entire gallery, then tags are also included for media objects.
    /// </summary>
    /// <param name="repo">The metadata tag repository.</param>
    /// <returns>A collection of <see cref="Entity.Tag" /> instances.</returns>
    /// <remarks>This function is similar to <see cref="GetTagsForMediaObjects(IRepository{MetadataTagDto})" />, so if a developer
    /// modifies it, be sure to check that function to see if it needs a similar change.</remarks>
    private IEnumerable<Entity.Tag> GetTagsForAlbums(IRepository<MetadataTagDto> repo)
    {
      var qry = repo.Where(
        m =>
        m.FKGalleryId == SearchOptions.GalleryId &&
        m.Metadata.MetaName == TagName);

      if (!String.IsNullOrEmpty(SearchOptions.SearchTerm))
      {
        qry = qry.Where(m => m.FKTagName.Contains(SearchOptions.SearchTerm));
      }

      if (SearchOptions.IsUserAuthenticated)
      {
        if (!UserCanViewRootAlbum)
        {
          // User can't view the root album, so get a list of the albums she *can* see and make sure our 
          // results only include albums that are viewable.
          var albumIds = SearchOptions.Roles.GetViewableAlbumIdsForGallery(SearchOptions.GalleryId).Cast<int?>();

          qry = qry.Where(a => albumIds.Contains(a.Metadata.FKAlbumId));
        }
      }
      else if (UserCanViewRootAlbum)
      {
        // Anonymous user, so don't include any private albums in results.
        qry = qry.Where(a => 
          (a.Metadata.Album != null && !a.Metadata.Album.IsPrivate) || 
          (a.Metadata.MediaObject != null && !a.Metadata.MediaObject.Album.IsPrivate));
      }
      else
      {
        // User is anonymous and does not have permission to view the root album, meaning they
        // can't see anything. Return empty collection.
        return new List<Entity.Tag>();
      }

      return qry.GroupBy(t => t.FKTagName).Select(t => new Entity.Tag { Value = t.Key, Count = t.Count() });
    }

    /// <summary>
    /// Gets the tags associated with media objects the user has permission to view.
    /// </summary>
    /// <param name="repo">The metadata tag repository.</param>
    /// <returns>A collection of <see cref="Entity.Tag" /> instances.</returns>
    /// <remarks>This function is similar to <see cref="GetTagsForAlbums(IRepository{MetadataTagDto})" />, so if a developer
    /// modifies it, be sure to check that function to see if it needs a similar change.</remarks>
    private IEnumerable<Entity.Tag> GetTagsForMediaObjects(IRepository<MetadataTagDto> repo)
    {
      var qry = repo.Where(
        m =>
        m.FKGalleryId == SearchOptions.GalleryId &&
        m.Metadata.MetaName == TagName);

      if (!String.IsNullOrEmpty(SearchOptions.SearchTerm))
      {
        qry = qry.Where(m => m.FKTagName.Contains(SearchOptions.SearchTerm));
      }

      if (SearchOptions.IsUserAuthenticated)
      {
        if (!UserCanViewRootAlbum)
        {
          // User can't view the root album, so get a list of the albums she *can* see and make sure our 
          // results only include albums that are viewable.
          var albumIds = SearchOptions.Roles.GetViewableAlbumIdsForGallery(SearchOptions.GalleryId);

          qry = qry.Where(a => albumIds.Contains(a.Metadata.MediaObject.FKAlbumId));
        }
      }
      else if (UserCanViewRootAlbum)
      {
        // Anonymous user, so don't include any private albums in results.
        qry = qry.Where(a => !a.Metadata.MediaObject.Album.IsPrivate);
      }
      else
      {
        // User is anonymous and does not have permission to view the root album, meaning they
        // can't see anything. Return empty collection.
        return new List<Entity.Tag>();
      }

      return qry.GroupBy(t => t.FKTagName).Select(t => new Entity.Tag { Value = t.Key, Count = t.Count() });
    }

    /// <summary>
    /// Sort and filter the <paramref name="tags" /> by the requested sort and filter options.
    /// </summary>
    /// <param name="tags">The tags.</param>
    /// <returns>IEnumerable{Entity.Tag}.</returns>
    private IEnumerable<Entity.Tag> SortAndFilter(IEnumerable<Entity.Tag> tags)
    {
      IEnumerable<Entity.Tag> newTags;

      switch (SearchOptions.SortProperty)
      {
        case TagSearchOptions.TagProperty.Value:
          newTags = SearchOptions.SortAscending ? tags.OrderBy(t => t.Value) : tags.OrderByDescending(t => t.Value);
          break;

        case TagSearchOptions.TagProperty.Count:
          newTags = SearchOptions.SortAscending ? tags.OrderBy(t => t.Count) : tags.OrderByDescending(t => t.Count);
          break;

        default:
          newTags = tags;
          break;
      }

      return (SearchOptions.NumTagsToRetrieve < int.MaxValue)
        ? newTags.Take(SearchOptions.NumTagsToRetrieve)
        : newTags;
    }

    /// <summary>
    /// Gets a list of all tags in the gallery, regardless of a user's permission.
    /// </summary>
    /// <returns>IEnumerable{TagDto}.</returns>
    private IEnumerable<Entity.Tag> GetTags()
    {
      using (var repo = new MetadataTagRepository())
      {
        var tags = repo.Where(t => t.FKGalleryId == SearchOptions.GalleryId && t.Metadata.MetaName == TagName)
          .GroupBy(t => t.FKTagName)
          .Select(t => new Entity.Tag { Value = t.Key, Count = t.Count() });

        return SortAndFilter(tags);
      }
    }

    #endregion
  }
}

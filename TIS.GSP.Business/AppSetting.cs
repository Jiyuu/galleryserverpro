using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using GalleryServerPro.Business.Interfaces;
using GalleryServerPro.Data;
using GalleryServerPro.Data.Migrations;

namespace GalleryServerPro.Business
{
	/// <summary>
	/// Contains application level settings used by Gallery Server Pro. This class must be initialized by the calling assembly early in the 
	/// application life cycle. It is initialized by calling <see cref="Initialize" />. In the case of the Gallery 
	/// Server Pro web application, <see cref="Initialize" /> is called from the static constructor of the GspPage base page.
	/// </summary>
	public class AppSetting : IAppSetting
	{
		#region Private Static Fields

		private static volatile IAppSetting _instance;
		private static readonly object _sharedLock = new object();

		#endregion

		#region Private Fields

		private int _mediaObjectDownloadBufferSize;
		private bool _encryptMediaObjectUrlOnClient;
		private string _encryptionKey;
		private string _jQueryScriptPath;
		private string _jQueryMigrateScriptPath;
		private string _jQueryUiScriptPath;
		private string _membershipProviderName;
		private string _roleProviderName;
		private ILicense _license;
		private bool _enableCache;
		private bool _allowGalleryAdminToManageUsersAndRoles;
		private bool _allowGalleryAdminViewAllUsersAndRoles;
		private int _maxNumberErrorItems;
		private string _emailFromName;
		private string _emailFromAddress;
		private string _smtpServer;
		private string _smtpServerPort;
		private bool _sendEmailUsingSsl;
		private string _tempUploadDirectory;
		private string _physicalAppPath;
		private string _applicationName;
		private ApplicationTrustLevel _trustLevel = ApplicationTrustLevel.None;
		private Version _dotNetFrameworkVersion;
		private string _iisAppPoolIdentity;
		private string _ffmpegPath;
		private string _imageMagickConvertPath;
		private bool _isInitialized;
		private MaintenanceStatus _maintenanceStatus = MaintenanceStatus.NotStarted;
		private readonly System.Collections.Specialized.StringCollection _verifiedFilePaths = new System.Collections.Specialized.StringCollection();
		private bool _installationRequested;
		private string _dataSchemaVersion;
		private string _galleryResourcesPath;

		#endregion

		#region Constructors

		private AppSetting()
		{
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets or sets the name of the skin.
		/// </summary>
		/// <value>The name of the skin.</value>
		public string Skin { get; set; }

		/// <summary>
		/// Gets or sets the size of each block of bytes when transferring files to streams and vice versa. This property was originally
		/// created to specify the buffer size for downloading a media object to the client, but it is now used for all
		/// file/stream copy operations.
		/// </summary>
		public int MediaObjectDownloadBufferSize
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _mediaObjectDownloadBufferSize;
			}
			set
			{
				_mediaObjectDownloadBufferSize = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether security-sensitive portions of the URL to the media object are encrypted when it is sent
		/// to the client browser. When false, the URL to the media object is sent in plain text, such as
		/// "handler/getmedia.ashx?moid=34&amp;dt=1&amp;g=1"
		/// These URLs can be seen by viewing the source of the HTML page. From this URL one can determine the album ID
		/// for this media object is 8, (aid=8), the file path to the media object on the server is
		/// C:\gs\mypics\birthday.jpeg, and the requested image is a thumbnail (dt=1, where 1 is the value of the
		/// GalleryServerPro.Business.DisplayObjectType enumeration for a thumbnail). For enhanced security, this property should
		/// be true, which uses Triple DES encryption to encrypt the the query string.
		/// It is recommended to set this to true except when you are	troubleshooting and it is useful to see the
		/// filename and path in the HTML source. The Triple DES algorithm uses the secret key specified in the
		/// <see cref="EncryptionKey"/> property.
		/// </summary>
		public bool EncryptMediaObjectUrlOnClient
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _encryptMediaObjectUrlOnClient;
			}
			set
			{
				_encryptMediaObjectUrlOnClient = value;
			}
		}

		/// <summary>
		/// Gets or sets the secret key used for the Triple DES algorithm. Applicable when the property <see cref="EncryptMediaObjectUrlOnClient"/> = true.
		/// The string must be 24 characters in length and be sufficiently strong so that it cannot be easily cracked.
		/// An exception is thrown by the .NET Framework if the key is considered weak. Change this to a value known only
		/// to you to prevent others from being able to decrypt.
		/// </summary>
		public string EncryptionKey
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _encryptionKey;
			}
			set
			{
				_encryptionKey = value;
			}
		}

		/// <summary>
		/// Gets or sets the absolute or relative path to the jQuery script file as stored in the application settings table.
		/// A relative path must be relative to the root of the web application and start with a tilde ("~"). An absolute path must be a full URI
		/// (e.g. http://ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js, //ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js).
		/// It is not valid to specify a UNC path, mapped drive path, or path to the local file system (e.g. "C:\scripts\jquery.js").
		/// Specify an empty string to indicate to GSP that the containing application is responsible for adding the jQuery
		/// reference. In this case, GSP will not attempt to add a jQuery reference.  Guaranteed to not return null.
		/// </summary>
		/// <value>
		/// The absolute or relative path to the jQuery script file as stored in the application settings table.
		/// </value>
		/// <remarks>The path is returned exactly how it appears in the database.</remarks>
		public string JQueryScriptPath
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _jQueryScriptPath;
			}
			set
			{
				_jQueryScriptPath = value ?? String.Empty;
			}
		}

		/// <summary>
		/// Gets or sets the absolute or relative path to the jQuery Migrate script file as stored in the application settings table.
		/// The jQuery Migrate Plugin is used to provide backwards compatibility when using jQuery 1.9 and higher.
		/// A relative path must be relative to the root of the web application and start with a tilde ("~"). An absolute path must be a full URI
		/// (e.g. http://code.jquery.com/jquery-migrate-1.0.0.min.js, //code.jquery.com/jquery-migrate-1.0.0.min.js).
		/// It is not valid to specify a UNC path, mapped drive path, or path to the local file system (e.g. "C:\scripts\jquery.js").
		/// Specify an empty string when the migrate plugin should not be used.  Guaranteed to not return null.
		/// </summary>
		/// <value>The absolute or relative path to the jQuery Migrate script file as stored in the application settings table.</value>
		/// <exception cref="Events"></exception>
		/// <remarks>The path is returned exactly how it appears in the database.</remarks>
		public string JQueryMigrateScriptPath
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _jQueryMigrateScriptPath;
			}
			set
			{
				_jQueryMigrateScriptPath = value ?? String.Empty;
			}
		}

		/// <summary>
		/// Gets or sets the absolute or relative path to the jQuery UI script file as stored in the application settings table.
		/// A relative path must be relative to the root of the web application and start with a tilde ("~"). An absolute path must be a full URI
		/// (e.g. http://ajax.googleapis.com/ajax/libs/jqueryui/1.9.1/jquery-ui.min.js.
		/// It is not valid to specify a UNC path, mapped drive path, or path to the local file system (e.g. "C:\scripts\jquery.js").
		/// Specify an empty string to indicate to GSP that the containing application is responsible for adding the jQuery UI
		/// reference. In this case, GSP will not attempt to add a jQuery reference. Guaranteed to not return null.
		/// </summary>
		/// <value>
		/// The absolute or relative path to the jQuery UI script file as stored in the application settings table.
		/// </value>
		/// <remarks>The path is returned exactly how it appears in the database.</remarks>
		public string JQueryUiScriptPath
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _jQueryUiScriptPath;
			}
			set
			{
				_jQueryUiScriptPath = value ?? String.Empty;
			}
		}

		/// <summary>
		/// Gets the data store currently being used for gallery data.
		/// </summary>
		/// <value>An instance of <see cref="ProviderDataStore" />.</value>
		public ProviderDataStore ProviderDataStore
		{
			get
			{
				if (Factory.GetConnectionStringSettings().ProviderName.StartsWith("System.Data.SqlServerCe"))
					return ProviderDataStore.SqlCe;
				else if (Factory.GetConnectionStringSettings().ProviderName.StartsWith("System.Data.SqlClient"))
					return ProviderDataStore.SqlServer;
				else
					return ProviderDataStore.Unknown;
			}
		}

		/// <summary>
		/// Gets or sets the name of the Membership provider for the gallery users. Optional. When not specified, the default provider specified
		/// in web.config is used.
		/// </summary>
		/// <remarks>The name of the Membership provider for the gallery users.</remarks>
		public string MembershipProviderName
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _membershipProviderName;
			}
			set
			{
				_membershipProviderName = value;
			}
		}

		/// <summary>
		/// Gets or sets the name of the Role provider for the gallery users. Optional. When not specified, the default provider specified
		/// in web.config is used.
		/// </summary>
		/// <remarks>The name of the Role provider for the gallery users.</remarks>
		public string RoleProviderName
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _roleProviderName;
			}
			set
			{
				_roleProviderName = value;
			}
		}

		/// <summary>
		/// Gets or sets the license for the current application.
		/// </summary>
		/// <value>The license for the current application.</value>
		public ILicense License
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _license;
			}
			set
			{
				_license = value;
			}
		}

		/// <summary>
		/// Gets or sets the product key for this installation of Gallery Server Pro.
		/// </summary>
		public string ProductKey
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _license.ProductKey;
			}
			set
			{
				_license = new License
				{
					ProductKey = value,
					InstallDate = GetFirstGalleryInstallationDate(),
				};

				_license.Validate(false);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether to store objects in a cache for quicker retrieval. This significantly improves
		/// performance, but cannot be used in web farms because the cache is local to each server and there is not a cross-server
		/// mechanism to expire the cache.
		/// </summary>
		public bool EnableCache
		{
			get { return _enableCache; }
			set { _enableCache = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether gallery administrators are allowed to create, edit, and delete users and roles.</summary>
		public bool AllowGalleryAdminToManageUsersAndRoles
		{
			get { return _allowGalleryAdminToManageUsersAndRoles; }
			set { _allowGalleryAdminToManageUsersAndRoles = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether gallery administrators are allowed to see users and roles that do not have 
		/// access to current gallery.</summary>
		public bool AllowGalleryAdminToViewAllUsersAndRoles
		{
			get { return _allowGalleryAdminViewAllUsersAndRoles; }
			set { _allowGalleryAdminViewAllUsersAndRoles = value; }
		}

		/// <summary>
		/// Indicates the maximum number of error objects to persist to the data store. When the number of errors exceeds this
		/// value, the oldest item is purged to make room for the new item. A value of zero means no limit is enforced.
		/// </summary>
		public int MaxNumberErrorItems
		{
			get { return _maxNumberErrorItems; }
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value", String.Format(CultureInfo.CurrentCulture, "Invalid MaxNumberErrorItems setting: The value must be between 0 and {0}. Instead, the value was {1}.", Int32.MaxValue, value));
				}

				_maxNumberErrorItems = value;
			}
		}

		/// <summary>
		/// The name associated with the <see cref="EmailFromAddress" /> email address. Emails sent from Gallery Server
		/// will appear to be sent from this person.
		/// </summary>
		/// <value>The name of the email from.</value>
		public string EmailFromName
		{
			get { return _emailFromName; }
			set { _emailFromName = value; }
		}

		/// <summary>
		/// The email address associated with <see cref="EmailFromName" />. Emails sent from Gallery Server
		/// will appear to be sent from this email address.
		/// </summary>
		/// <value>The email from address.</value>
		/// <exception cref="System.ArgumentOutOfRangeException">value</exception>
		public string EmailFromAddress
		{
			get { return _emailFromAddress; }
			set
			{
				if (!HelperFunctions.IsValidEmail(value))
				{
					throw new ArgumentOutOfRangeException("value", String.Format(CultureInfo.CurrentCulture, "Invalid EmailFromAddress setting: The value must be a valid e-mail address. Instead, the value was {0}.", value));
				}

				_emailFromAddress = value;
			}
		}

		/// <summary>
		/// Specifies the IP address or name of the SMTP server used to send emails. (Examples: 127.0.0.1, 
		/// Godzilla, mail.yourisp.com) This value will override the SMTP server setting that may be in the 
		/// system.net mailSettings section of the web.config file (either explicitly or inherited from a 
		/// parent web.config file). Leave this setting blank to use the value in web.config or if you are 
		/// not using the email functionality.
		/// </summary>
		public string SmtpServer
		{
			get { return _smtpServer; }
			set { _smtpServer = value; }
		}

		/// <summary>
		/// Specifies the SMTP server port number used to send emails. This value will override the SMTP 
		/// server port setting that may be in the system.net mailSettings section of the web.config file 
		/// (either explicitly or inherited from a parent web.config file). Leave this setting blank to 
		/// use the value in web.config or if you are not using the email functionality. Defaults to 25 
		/// if not specified here or in web.config.
		/// </summary>
		public string SmtpServerPort
		{
			get { return _smtpServerPort; }
			set { _smtpServerPort = value; }
		}

		/// <summary>
		/// Specifies whether e-mail functionality uses Secure Sockets Layer (SSL) to encrypt the connection.
		/// </summary>
		public bool SendEmailUsingSsl
		{
			get { return _sendEmailUsingSsl; }
			set { _sendEmailUsingSsl = value; }
		}
		/// <summary>
		/// Gets the physical application path of the currently running application. For web applications this will be equal to
		/// the Request.PhysicalApplicationPath property.
		/// </summary>
		public string PhysicalApplicationPath
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _physicalAppPath;
			}
			protected set
			{
				this._physicalAppPath = value;
			}
		}

		/// <summary>
		/// Gets the trust level of the currently running application. 
		/// </summary>
		public ApplicationTrustLevel AppTrustLevel
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _trustLevel;
			}
			protected set
			{
				this._trustLevel = value;
			}
		}

		/// <summary>
		/// Gets the name of the currently running application. Default is "Gallery Server Pro".
		/// </summary>
		public string ApplicationName
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _applicationName;
			}
			protected set { _applicationName = value; }
		}

		/// <summary>
		/// Gets or sets the path, relative to the current application, to the directory
		/// containing the Gallery Server Pro resources such as images, user controls,
		/// scripts, etc. When setting the property, the following scrubbing occurs: (a) leading
		/// or trailing slashes are removed, (b) forward slashes ('/') are replaced with path 
		/// separator characters (i.e. the backward slash '\'). The result should be equivalent to
		/// the value of the galleryResourcesPath setting in the galleryServerPro/core section 
		/// of web.config. Examples: "gs", "GalleryServerPro\resources"
		/// </summary>
		/// <value>A string.</value>
		public string GalleryResourcesPath
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _galleryResourcesPath;
			}
			protected set
			{
				if (value != null)
				{
					value = value.Trim(new char[] { Path.DirectorySeparatorChar, '/' }).Replace('/', Path.DirectorySeparatorChar);
				}

				_galleryResourcesPath = value;
			}
		}

		/// <summary>
		/// Gets the full physical path to the directory where files can be temporarily stored. Example:
		/// "C:\inetpub\wwwroot\galleryserverpro\App_Data\_Temp"
		/// </summary>
		public string TempUploadDirectory
		{
			get
			{
				if (!this._isInitialized)
				{
					throw new Events.CustomExceptions.ApplicationNotInitializedException();
				}

				return _tempUploadDirectory;
			}
			protected set
			{
				// Validate the path. Will throw an exception if a problem is found.
				try
				{
					if (!this._verifiedFilePaths.Contains(value))
					{
						HelperFunctions.ValidatePhysicalPathExistsAndIsReadWritable(value);
						this._verifiedFilePaths.Add(value);
					}
				}
				catch (Events.CustomExceptions.CannotWriteToDirectoryException)
				{
					// Mark this app as not initialized so when user attempts to fix issue and refreshes the page, the initialize 
					// sequence will run again.
					this._isInitialized = false;
					throw;
				}

				this._tempUploadDirectory = value;
			}
		}

		/// <summary>
		/// Gets the .NET Framework version the current application is running under. Contains only the major and minor components.
		/// </summary>
		/// <value>
		/// The .NET Framework version the current application is running under.
		/// </value>
		/// <example>
		/// To verify the current application is running 3.0 or higher, use this:
		/// <code>
		/// if (AppSetting.Instance.DotNetFrameworkVersion &gt; new Version("2.0"))
		/// { /* App is 3.0 or higher */ }
		/// </code>
		/// </example>
		public Version DotNetFrameworkVersion
		{
			get
			{
				return this._dotNetFrameworkVersion;
			}
		}

		/// <summary>
		/// Gets the IIS application pool identity.
		/// </summary>
		/// <value>The application app pool identity.</value>
		public string IisAppPoolIdentity
		{
			get
			{
				if (_iisAppPoolIdentity == null)
				{
					WindowsIdentity identity = WindowsIdentity.GetCurrent();
					_iisAppPoolIdentity = (identity != null ? identity.Name : String.Empty);
				}

				return this._iisAppPoolIdentity;
			}
		}

		/// <summary>
		/// Gets the full file path to the FFmpeg utility. During application initialization the bin directory is inspected for the
		/// presence of ffmpeg.exe. If present, this property is assigned the value of the full path to the utility. If not present,
		/// the property is assigned <see cref="string.Empty" />. FFmpeg is used to extract thumbnails from videos and for video conversion.
		/// Example: C:\inetpub\wwwroot\gallery\bin\ffmpeg.exe
		/// </summary>
		/// <value>
		/// 	Returns the full file path to the FFmpeg utility, or <see cref="string.Empty" /> if the utility is not present.
		/// </value>
		public string FFmpegPath
		{
			get { return this._ffmpegPath; }
		}

		/// <summary>
		/// Gets the full file path to the ImageMagick convert.exe utility. During application initialization the bin directory is inspected for the
		/// presence of convert.exe. If present, this property is assigned the value of the full path to the utility. If not present,
		/// the property is assigned <see cref="string.Empty" />. This utility is used to extract thumbnails from .eps and .pdf files.
		/// Example: C:\inetpub\wwwroot\gallery\bin\convert.exe
		/// </summary>
		/// <value>
		/// 	Returns the full file path to the ImageMagick convert.exe utility, or <see cref="string.Empty" /> if the utility is not present.
		/// </value>
		public string ImageMagickConvertPath
		{
			get { return this._imageMagickConvertPath; }
		}

		/// <summary>
		/// Gets or sets the version of the objects in the database as reported by the database. Ex: "2.4.1"
		/// </summary>
		/// <value>The version of the objects in the database as reported by the database.</value>
		public string DataSchemaVersion
		{
			get { return _dataSchemaVersion; }
			set { _dataSchemaVersion = value; }
		}

		/// <summary>
		/// Gets a value indicating whether the current library has been populated with data from the calling assembly.
		/// This library is initialized by calling <see cref="Initialize" />.
		/// </summary>
		public bool IsInitialized
		{
			get
			{
				return _isInitialized;
			}
		}

		/// <summary>
		/// Gets or sets the maintenance status. During each application restart a maintenance routine is run that helps
		/// ensure data integrity and eliminate unused data. This property describes the status of the maintenance routine.
		/// </summary>
		/// <value>The maintenance status.</value>
		public MaintenanceStatus MaintenanceStatus
		{
			get { return _maintenanceStatus; }
			set { _maintenanceStatus = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether an installation is being requested. This value will be <c>true</c> when a text
		/// file named install.txt is detected in the App_Data directory. This property may be set during application initialization 
		/// so that later in the code path, when the gallery ID is available, the objects can be created.
		/// </summary>
		/// <value><c>true</c> if an installation is being requested; otherwise, <c>false</c>.</value>
		public bool InstallationRequested
		{
			get { return this._installationRequested; }
			set { this._installationRequested = value; }
		}

		#endregion

		#region Public Static Properties

		/// <summary>
		/// Gets a reference to the <see cref="AppSetting" /> singleton for this app domain.
		/// </summary>
		public static IAppSetting Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (_sharedLock)
					{
						if (_instance == null)
						{
							IAppSetting tempAppSetting = new AppSetting();

							// Ensure that writes related to instantiation are flushed.
							System.Threading.Thread.MemoryBarrier();
							_instance = tempAppSetting;
						}
					}
				}

				return _instance;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Assign various application-wide properties to be used during the lifetime of the application. This method
		/// should be called once when the application first starts.
		/// </summary>
		/// <param name="trustLevel">The trust level of the current application.</param>
		/// <param name="physicalAppPath">The physical path of the currently executing application. For web applications
		/// this will be equal to the Request.PhysicalApplicationPath property.</param>
		/// <param name="appName">The name of the currently running application.</param>
		/// <param name="galleryResourcesPath">The path, relative to the current application, to 
		/// the directory containing the Gallery Server Pro resources such as images, user controls, 
		/// scripts, etc. Use the value of the galleryResourcesPath setting in the 
		/// galleryServerPro/core section of web.config. Examples: "gs", "GalleryServerPro\resources"</param>
		/// <exception cref="System.InvalidOperationException">Thrown when this method is called more than once during
		/// the application's lifetime.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">Thrown if the trustLevel parameter has the value
		/// ApplicationTrustLevel.None.</exception>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="physicalAppPath"/> or <paramref name="appName"/>
		/// is null.</exception>
		/// <exception cref="GalleryServerPro.Events.CustomExceptions.CannotWriteToDirectoryException">
		/// Thrown when Gallery Server Pro is unable to write to, or delete from, a directory. This may be the media objects
		/// directory, thumbnail or optimized directory, the temporary directory (defined in
		/// <see cref="GlobalConstants.TempUploadDirectory"/>), or the App_Data directory.</exception>
		public void Initialize(ApplicationTrustLevel trustLevel, string physicalAppPath, string appName, string galleryResourcesPath)
		{
			#region Validation

			if (this._isInitialized)
			{
				throw new System.InvalidOperationException("The AppSetting instance has already been initialized. It cannot be initialized more than once.");
			}

			if (trustLevel == ApplicationTrustLevel.None)
			{
				throw new System.ComponentModel.InvalidEnumArgumentException("Invalid ApplicationTrustLevel value. ApplicationTrustLevel.None is not valid. Use ApplicationTrustLevel.Unknown if the trust level cannot be calculated.");
			}

			if (String.IsNullOrEmpty(physicalAppPath))
				throw new ArgumentNullException("physicalAppPath");

			if (String.IsNullOrEmpty(appName))
				throw new ArgumentNullException("appName");

			#endregion

			this.AppTrustLevel = trustLevel;
			this.PhysicalApplicationPath = physicalAppPath;
			this.ApplicationName = appName;
			this.GalleryResourcesPath = galleryResourcesPath;

			ConfigureAppDataDirectory(physicalAppPath);

			InitializeDataStore();

			PopulateAppSettingsFromDataStore();

			ConfigureTempDirectory(physicalAppPath);

			this._dotNetFrameworkVersion = GetDotNetFrameworkVersion();

			string ffmpegPath = Path.Combine(physicalAppPath, @"bin\ffmpeg.exe");
			this._ffmpegPath = (File.Exists(ffmpegPath) ? ffmpegPath : String.Empty);

			string imageMagickConvertPath = Path.Combine(physicalAppPath, @"bin\convert.exe");
			this._imageMagickConvertPath = (File.Exists(imageMagickConvertPath) ? imageMagickConvertPath : String.Empty);

			this._isInitialized = true;

			// Validate the application and gallery settings. This must come after setting _isInitialized to true because the function 
			// accesses properties of the AppSetting singleton, which will throw a ApplicationNotInitializedException when a property is 
			// accessed before initialization is complete.
			Validate();
		}

		/// <summary>
		/// Persist the specified application settings to the data store. Specify a null value for each parameter whose value is
		/// not changing.
		/// </summary>
		/// <param name="license">A license instance containing the product key for this installation of Gallery Server Pro. The
		/// product key must be validated before invoking this method.</param>
		/// <param name="skin">The name of the skin.</param>
		/// <param name="mediaObjectDownloadBufferSize">The size of each block of bytes when transferring files to streams and vice versa.</param>
		/// <param name="encryptMediaObjectUrlOnClient">Indicates whether security-sensitive portions of the URL to the media object are
		/// encrypted when it is sent to the client browser.</param>
		/// <param name="encryptionKey">The secret key used for the Triple DES algorithm.</param>
		/// <param name="jQueryScriptPath">The absolute or relative path to the jQuery script file.</param>
		/// <param name="jQueryMigrateScriptPath">The absolute or relative path to the jQuery Migrate script file.</param>
		/// <param name="jQueryUiScriptPath">The absolute or relative path to the jQuery UI script file.</param>
		/// <param name="membershipProviderName">The name of the Membership provider for the gallery users.</param>
		/// <param name="roleProviderName">The name of the Role provider for the gallery users.</param>
		/// <param name="enableCache">Indicates whether to store objects in a cache for quicker retrieval.</param>
		/// <param name="allowGalleryAdminToManageUsersAndRoles">Indicates whether gallery administrators are allowed to create, edit, and delete
		/// users and roles.</param>
		/// <param name="allowGalleryAdminViewAllUsersAndRoles">Indicates whether gallery administrators are allowed to see users and roles that
		/// do not have access to current gallery.</param>
		/// <param name="maxNumberErrorItems">The maximum number of error objects to persist to the data store.</param>
		/// <param name="emailFromName">The name associated with the <paramref name="emailFromAddress" /> email address. Emails sent from Gallery Server
		/// will appear to be sent from this person.</param>
		/// <param name="emailFromAddress">The email address associated with <paramref name="emailFromName" />. Emails sent from Gallery Server
		/// will appear to be sent from this email address.</param>
		/// <param name="smtpServer">Specifies the IP address or name of the SMTP server used to send emails. (Examples: 127.0.0.1,
		/// Godzilla, mail.yourisp.com)</param>
		/// <param name="smtpServerPort">Specifies the SMTP server port number used to send emails.</param>
		/// <param name="sendEmailUsingSsl">Specifies whether e-mail functionality uses Secure Sockets Layer (SSL) to encrypt the connection.</param>
		public void Save(ILicense license, string skin, int? mediaObjectDownloadBufferSize, bool? encryptMediaObjectUrlOnClient, string encryptionKey, string jQueryScriptPath, string jQueryMigrateScriptPath, string jQueryUiScriptPath, string membershipProviderName, string roleProviderName, bool? enableCache, bool? allowGalleryAdminToManageUsersAndRoles, bool? allowGalleryAdminViewAllUsersAndRoles, int? maxNumberErrorItems, string emailFromName, string emailFromAddress, string smtpServer, string smtpServerPort, bool? sendEmailUsingSsl)
		{
			bool productKeyWasChanged = false;

			lock (_sharedLock)
			{
				if (license != null)
				{
					productKeyWasChanged = (License.ProductKey != license.ProductKey);
					License = license;

					ValidateLicenseTypeConfiguration();
				}

				if (!String.IsNullOrEmpty(skin))
					Skin = skin;

				if (mediaObjectDownloadBufferSize.HasValue)
					MediaObjectDownloadBufferSize = mediaObjectDownloadBufferSize.Value;

				if (encryptMediaObjectUrlOnClient.HasValue)
					EncryptMediaObjectUrlOnClient = encryptMediaObjectUrlOnClient.Value;

				if (!String.IsNullOrEmpty(encryptionKey))
					EncryptionKey = encryptionKey;

				if (jQueryScriptPath != null)
					JQueryScriptPath = jQueryScriptPath;

				if (jQueryScriptPath != null)
					JQueryMigrateScriptPath = jQueryMigrateScriptPath;

				if (jQueryUiScriptPath != null)
					JQueryUiScriptPath = jQueryUiScriptPath;

				if (!String.IsNullOrEmpty(membershipProviderName))
					MembershipProviderName = membershipProviderName;

				if (!String.IsNullOrEmpty(roleProviderName))
					RoleProviderName = roleProviderName;

				if (enableCache.HasValue)
					EnableCache = enableCache.Value;

				if (allowGalleryAdminToManageUsersAndRoles.HasValue)
					AllowGalleryAdminToManageUsersAndRoles = allowGalleryAdminToManageUsersAndRoles.Value;

				if (allowGalleryAdminViewAllUsersAndRoles.HasValue)
					AllowGalleryAdminToViewAllUsersAndRoles = allowGalleryAdminViewAllUsersAndRoles.Value;

				if (maxNumberErrorItems.HasValue)
					MaxNumberErrorItems = maxNumberErrorItems.Value;

				if (emailFromName != null)
					EmailFromName = emailFromName;

				if (emailFromAddress != null)
					EmailFromAddress = emailFromAddress;

				if (smtpServer != null)
					SmtpServer = smtpServer;

				if (smtpServerPort != null)
					SmtpServerPort = smtpServerPort;

				if (sendEmailUsingSsl.HasValue)
					SendEmailUsingSsl = sendEmailUsingSsl.Value;

				//Factory.GetDataProvider().AppSetting_Save(this);
				using (var repo = new AppSettingRepository())
				{
					repo.Save(this);
				}

				if (productKeyWasChanged)
				{
					Factory.ClearWatermarkCache(); //Changing the product key might cause a different watermark to be rendered
				}
			}
		}

		private void PopulateAppSettingsFromDataStore()
		{
			var asType = typeof(AppSetting);

			using (var repo = new AppSettingRepository())
			{
				foreach (var appSettingDto in repo.GetAll())
				{
					var prop = asType.GetProperty(appSettingDto.SettingName);

					if (prop == null)
					{
						throw new MissingMemberException(String.Format(CultureInfo.CurrentCulture, "Invalid application setting. An application setting named '{0}' was found in the data store, but no property by that name exists in the class '{1}'. Check the application settings in the data store to ensure they are correct.", appSettingDto.SettingName, asType));
					}
					else if (prop.PropertyType == typeof(bool))
					{
						prop.SetValue(this, Convert.ToBoolean(appSettingDto.SettingValue.Trim(), CultureInfo.InvariantCulture), null);
					}
					else if (prop.PropertyType == typeof(string))
					{
						prop.SetValue(this, Convert.ToString(appSettingDto.SettingValue.Trim(), CultureInfo.InvariantCulture), null);
					}
					else if (prop.PropertyType == typeof(int))
					{
						prop.SetValue(this, Convert.ToInt32(appSettingDto.SettingValue.Trim(), CultureInfo.InvariantCulture), null);
					}
					else
					{
						throw new ArgumentOutOfRangeException(String.Format(CultureInfo.CurrentCulture, "AppSetting.PopulateAppSettingsFromDataStore is not designed to process a property of type {0} (encountered in AppSetting.{1})", prop.PropertyType, prop.Name));
					}
				}
			}
		}

		private void InitializeDataStore()
		{
			System.Data.Entity.Database.SetInitializer(new System.Data.Entity.MigrateDatabaseToLatestVersion<GalleryDb, GalleryDbMigrationConfiguration>());

			var configuration = new GalleryDbMigrationConfiguration(ProviderDataStore);
			var migrator = new System.Data.Entity.Migrations.DbMigrator(configuration);
			if (migrator.GetPendingMigrations().Any())
			{
				migrator.Update();
			}
		}

		private void ConfigureAppDataDirectory(string physicalAppPath)
		{
			// Validate that the App_Data path is read-writeable. Will throw an exception if a problem is found.
			string appDataDirectory = Path.Combine(physicalAppPath, GlobalConstants.AppDataDirectory);
			try
			{
				HelperFunctions.ValidatePhysicalPathExistsAndIsReadWritable(appDataDirectory);
			}
			catch (GalleryServerPro.Events.CustomExceptions.CannotWriteToDirectoryException)
			{
				// Mark this app as not initialized so when user attempts to fix issue and refreshes the page, the initialize 
				// sequence will run again.
				this._isInitialized = false;
				throw;
			}
		}

		private void ConfigureTempDirectory(string physicalAppPath)
		{
			this.TempUploadDirectory = Path.Combine(physicalAppPath, GlobalConstants.TempUploadDirectory);

			try
			{
				// Clear out all directories and files in the temp directory. If an IOException error occurs, perhaps due to a locked file,
				// record it but do not let it propagate up the stack.
				DirectoryInfo di = new DirectoryInfo(this._tempUploadDirectory);
				foreach (FileInfo file in di.GetFiles())
				{
					if ((file.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
					{
						file.Delete();
					}
				}
				foreach (DirectoryInfo dirInfo in di.GetDirectories())
				{
					if ((dirInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
					{
						dirInfo.Delete(true);
					}
				}
			}
			catch (IOException ex)
			{
				GalleryServerPro.Events.EventController.RecordError(ex, this);
				HelperFunctions.PurgeCache();
			}
			catch (UnauthorizedAccessException ex)
			{
				GalleryServerPro.Events.EventController.RecordError(ex, this);
				HelperFunctions.PurgeCache();
			}
		}

		private static Version GetDotNetFrameworkVersion()
		{
			return new Version(Environment.Version.ToString(2));
		}

		/// <summary>
		/// Validate the application and gallery settings.
		/// </summary>
		private void Validate()
		{
			ValidateGalleries();
		}

		/// <summary>
		/// Verifies each gallery has  the required minimum number of records, creating them if necessary.
		/// This function does not create a gallery.
		/// </summary>
		private static void ValidateGalleries()
		{
			foreach (var gallery in Factory.LoadGalleries())
			{
				gallery.Configure();
			}
		}

		/// <summary>
		/// Gets the date/time when the first gallery in the database was created. For practical purposes we can consider this the date 
		/// the application was installed. If no galleries have been created (which may happen the first time we run the app), just
		/// return today's date.
		/// </summary>
		/// <returns>Returns a <see cref="DateTime" /> representing when the first gallery in the database was created.</returns>
		private static DateTime GetFirstGalleryInstallationDate()
		{
			DateTime firstGalleryInstallDate = DateTime.Today;

			foreach (IGallery gallery in Factory.LoadGalleries())
			{
				if (gallery.CreationDate < firstGalleryInstallDate)
				{
					firstGalleryInstallDate = gallery.CreationDate;
				}
			}

			return firstGalleryInstallDate;
		}

		/// <summary>
		/// Verifies the application is correctly configured based on the current license type. Specifically, it verifies that
		/// additional UI templates are present for Enterprise license holders.
		/// </summary>
		private void ValidateLicenseTypeConfiguration()
		{
			if (License.LicenseType == LicenseLevel.Enterprise)
			{
				SeedController.InsertEnterpriseTemplates();

				HelperFunctions.RemoveCache(CacheItem.UiTemplates);

				// The validation will make a copy of each new UI template for every gallery.
				Validate();
			}
		}

		#endregion
	}
}

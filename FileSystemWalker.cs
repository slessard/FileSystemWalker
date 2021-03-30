using System;
using System.Diagnostics;
using System.IO;

namespace com.pigdawg.utils.FileSystem
{
    public enum FileSystemWalkerDiagnostic
    {
        None = 0,
        ProcessingStarted,
        FileProcessing,
        FileReparsePoint,
        FileFailed,
        FileRead,
        DirectoryProcessing,
        DirectoryReparsePoint,
        DirectoryFailed,
        DirectoryRead,
        UnknownObject,
        ProcessingCompleted,
    }
    
    public class FileSystemWalkerDiagnosticEventArgs : EventArgs
    {
        private FileSystemWalkerDiagnostic m_diagnostic;
        
        internal FileSystemWalkerDiagnosticEventArgs(FileSystemWalkerDiagnostic diagnostic)
        {
            m_diagnostic = diagnostic;
        }
        
        public FileSystemWalkerDiagnostic Diagnostic
        {
            get
            {
                return this.m_diagnostic;
            }
        }
    }
    
    public class DirectoryFoundEventArgs : EventArgs
    {
        private DirectoryInfo m_info;
        
        public DirectoryFoundEventArgs(DirectoryInfo info)
        {
            m_info = info;
        }
        
        public DirectoryInfo DirectoryInfo
        {
            get
            {
                return m_info;
            }
        }
    }
    
    public class FileFoundEventArgs : EventArgs
    {
        private FileInfo m_info;
        
        public FileFoundEventArgs(FileInfo info)
        {
            m_info = info;
        }
        
        public FileInfo FileInfo
        {
            get
            {
                return m_info;
            }
        }
    }
    
    /// <summary>
    /// Settings for specifying the behavior of an instance of the FileSystemWalker class
    /// </summary>
    public class FileSystemWalkerSettings
    {
        private bool m_followDirectorySymLinks;
        private bool m_followFileSymLinks;
        private bool m_recurseDirectories;
        
        public FileSystemWalkerSettings()
        {
        }
        
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name='other'>
        /// The instance of the <see cref="com.pigdawg.utils.FileSystem.FileSystemWalkerSettings"/> class to be copied
        /// </param>
        internal FileSystemWalkerSettings(FileSystemWalkerSettings other)
        {
            m_followDirectorySymLinks = other.FollowDirectorySymLinks;
            m_followFileSymLinks = other.FollowFileSymLinks;
            m_recurseDirectories = other.RecurseDirectories;
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether directory symbolic links (reparse points) should be followed.
        /// </summary>
        /// <value>
        /// <c>true</c> if directory symbolic links should be followed; otherwise, <c>false</c>.
        /// </value>
        public bool FollowDirectorySymLinks
        {
            get { return m_followDirectorySymLinks; }
            set { m_followDirectorySymLinks = value; }
        }
        
        /// <summary>
        /// Gets or sets a value indicating whether file symbolic links (reparse points) should be followed.
        /// </summary>
        /// <value>
        /// <c>true</c> if file symbolic links should be followed; otherwise, <c>false</c>.
        /// </value>
        public bool FollowFileSymLinks
        {
            get { return m_followFileSymLinks; }
            set { m_followFileSymLinks = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether directories should be recursed
        /// </summary>
        /// <value>
        /// <c>true</c> if directories should be recursed; otherwise, <c>false</c>.
        /// </value>
        public bool RecurseDirectories
        {
            get { return m_recurseDirectories; }
            set { m_recurseDirectories = value; }
        }
    }
    
    public partial class FileSystemWalker
    {
        private readonly FileSystemWalkerSettings m_settings;
        
        public FileSystemWalker (FileSystemWalkerSettings settings)
        {
            // Copy the settings so that this instance of FileSystemWalker will be
            // immune to any changes the application may make to the settings while
            // this instance is operating
            m_settings = new FileSystemWalkerSettings(settings);
        }

        public event EventHandler<FileSystemWalkerDiagnosticEventArgs> DiagnosticDetected;
        public event EventHandler<FileFoundEventArgs> FileFound;
        public event EventHandler<DirectoryFoundEventArgs> DirectoryFound;
        
        public void Run<TInfo> (TInfo inputPath) where TInfo : FileSystemInfo
        {
            RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.ProcessingStarted);
                
            try
            {
                DirectoryInfo di = inputPath as DirectoryInfo;
                FileInfo fi = inputPath as FileInfo;
                
                Debug.Assert(((null != di) || (null != fi)), "di and fi are both null");
                
                if (null != di)
                {
                    ProcessDirectory(di);
                }
                else
                {
                    ProcessFile(fi);
                }
            }
            finally
            {
                RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.ProcessingCompleted);
            }
        }
                    
        private void ProcessDirectory (DirectoryInfo dirInfo)
        {
            if (dirInfo.Exists)
            {
                bool isSymLink = false;
                
                this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.DirectoryProcessing);
    
                isSymLink = (0 != (dirInfo.Attributes & FileAttributes.ReparsePoint));
                
                if (isSymLink)
                {
                    this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.DirectoryReparsePoint);
                }
                
                if (!isSymLink || m_settings.FollowDirectorySymLinks)
                {
                    try
                    {
						this.RaiseDirectoryFoundEvent(dirInfo);
							
						FileInfo[] files = dirInfo.GetFiles();
                        DirectoryInfo[] subdirs = dirInfo.GetDirectories();
                        
                        if (0 < files.Length)
                        {
                            Array.ForEach<FileInfo>(files, file => { ProcessFile(file); });
                        }
                        
                        if (m_settings.RecurseDirectories &&
                            (0 < subdirs.Length))
                        {
                            Array.ForEach<DirectoryInfo>(subdirs, dir => { ProcessDirectory(dir); });
                        }
                        
                        this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.DirectoryRead);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error processing directory: \"{0}\"", dirInfo.FullName);
                        Console.Error.WriteLine(ex.ToString());

                        this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.DirectoryFailed);
                    }
                }
            }
            else
            {
                string message = string.Format("Path does not exist: \"{0}\"", dirInfo.FullName);

                System.Console.Error.WriteLine(message);

                this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.UnknownObject);
            }
        }

        private void ProcessFile (FileInfo fileInfo)
        {
            if (fileInfo.Exists)
            {
                bool isSymLink = false;
                
                this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.FileProcessing);

                isSymLink = (0 != (fileInfo.Attributes & FileAttributes.ReparsePoint));
                
                if (isSymLink)
                {
                    this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.FileReparsePoint);
                }

                if (!isSymLink || m_settings.FollowFileSymLinks)
                {
                    Exception caughtException = null;
                    
                    try
                    {
                        this.RaiseFileFoundEvent(fileInfo);

                        // If no exception was thrown above then assume the file was successfully read.
                        this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.FileRead);
                    }
                    catch (System.UnauthorizedAccessException ex)
                    {
                        caughtException = ex;
                        Console.Error.WriteLine("Access Denied: \"{0}\"", fileInfo.FullName);
                        
                        // UnauthorizedAccessException is expected and not critical therefore
                        // it's stack trace does not need to be dumped

                        this.DumpAccessControls(fileInfo);
                    }
                    catch (Exception ex)
                    {
                        caughtException = ex;
                        Console.Error.WriteLine("Error processing file: \"{0}\"", fileInfo.FullName);
                        Console.Error.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        if (null != caughtException)
                        {
                            this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.FileFailed);
                        }
                    }
                }
            }
            else
            {
                string message = string.Format("Unknown file system object: \"{0}\"", fileInfo.FullName);

                System.Console.Error.WriteLine(message);
    
                // Hitting an unknown file system object would be such a big deal
                // that it should be investigated immediately
                Debug.Fail(message);

                this.RaiseDiagnosticEvent(FileSystemWalkerDiagnostic.UnknownObject);
            }
        }

        private void RaiseDiagnosticEvent(FileSystemWalkerDiagnostic diagnostic)
        {
            EventHandler<FileSystemWalkerDiagnosticEventArgs> eventListeners = this.DiagnosticDetected;
            if (null != eventListeners)
            {
                FileSystemWalkerDiagnosticEventArgs eventArgs = new FileSystemWalkerDiagnosticEventArgs(diagnostic);
                eventListeners(this, eventArgs);
            }
        }

        private void RaiseDirectoryFoundEvent(DirectoryInfo info)
        {
            EventHandler<DirectoryFoundEventArgs> eventListeners = this.DirectoryFound;
            if (null != eventListeners)
            {
                DirectoryFoundEventArgs eventArgs = new DirectoryFoundEventArgs(info);
                eventListeners(this, eventArgs);
            }
        }

        private void RaiseFileFoundEvent(FileInfo info)
        {
            EventHandler<FileFoundEventArgs> eventListeners = this.FileFound;
            if (null != eventListeners)
            {
                FileFoundEventArgs eventArgs = new FileFoundEventArgs(info);
                eventListeners(this, eventArgs);
            }
        }
    }
}


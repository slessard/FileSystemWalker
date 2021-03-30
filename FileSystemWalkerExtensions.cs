using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace com.pigdawg.utils.FileSystem
{
    public partial class FileSystemWalker
    {
        [Conditional("WINDOWS_BUILD")]
        private void DumpAccessControls(FileInfo info)
        {
            try
            {
                IdentityReference idref = null;
                FileSecurity fileSecurity = null;
                
                // Get a FileSecurity object that represents the 
                // current security settings.
                fileSecurity = info.GetAccessControl();
    
                // Try to get the NTAccount identity reference
                try
                {
                    // Available arguments for the GetGroup method are:
                    // typeof(System.Security.Principal.NTAccount)
                    // typeof(System.Security.Principal.SecurityIdentifier)
                    idref = fileSecurity.GetGroup(typeof(System.Security.Principal.NTAccount));
                    
                    System.Console.WriteLine(idref.ToString());
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine(ex.Message);
                }
        
                // If the NTAccount identity reference couldn't be retrieved try to get 
                // the SecurityIdentifier identity reference
                if (null == idref)
                {
                    try
                    {
                        // Available arguments for the GetGroup method are:
                        // typeof(System.Security.Principal.NTAccount)
                        // typeof(System.Security.Principal.SecurityIdentifier)
                        idref = fileSecurity.GetGroup(typeof(System.Security.Principal.SecurityIdentifier));
                    
                        System.Console.WriteLine(idref.ToString());
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine(ex.Message);
                    }
                }
            }
            catch (NotImplementedException nex)
            {
                // The Access controls can't be read on this runtime (probably Mono)
                // Log it and move on.
                
                string stackTrace = nex.StackTrace;
                Match feature = Regex.Match(stackTrace, @"at ([^\[]+)\[0x");
                Match location = Regex.Match(stackTrace, @"\] in (.*)$");
                if (feature.Success && location.Success)
                {
                    string featureName = feature.Groups[1].ToString().Trim();
                    string featureLocation = location.Groups[1].ToString().Trim();
                    System.Console.Error.WriteLine("{0}: '{1}' at {2}", nex.Message, featureName, featureLocation);
                }
                else
                {
                    System.Console.Error.WriteLine("Feature not implemented: {0}", stackTrace);
                }
            }
        }
    }
}

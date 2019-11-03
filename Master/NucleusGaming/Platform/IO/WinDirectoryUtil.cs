using Nucleus.Gaming.Platform.Windows.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nucleus.Gaming.Platform.Windows.IO
{
    public static class WinDirectoryUtil
    {
        public static void LinkFiles(string rootFolder, string destination, out int exitCode, string[] exclusions, string[] copyInstead, bool hardLink)
        {
            exitCode = 1;

            FileInfo[] files = new DirectoryInfo(rootFolder).GetFiles();

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = files[i];

                string lower = file.Name.ToLower();
                bool exclude = false;

                if (!string.IsNullOrEmpty(exclusions[0]))
                {
                    for (int j = 0; j < exclusions.Length; j++)
                    {
                        string exc = exclusions[j];
                        if (!string.IsNullOrEmpty(exc) && lower.Equals(exc))
                        {
                            // check if the file is i
                            exclude = true;
                            break;
                        }
                    }
                }

                if (exclude)
                {
                    continue;
                }

                if(!string.IsNullOrEmpty(copyInstead[0]))
                {
                    for (int j = 0; j < copyInstead.Length; j++)
                    {
                        string copy = copyInstead[j];
                        if (!string.IsNullOrEmpty(copy) && lower.Equals(copy))
                        {
                            exclude = true;
                            break;
                        }
                    }
                }

                string relative = file.FullName.Replace(rootFolder + @"\", "");
                string linkPath = Path.Combine(destination, relative);
                if (exclude)
                {
                    // should copy!
                    File.Copy(file.FullName, linkPath, true);
                }
                else
                {
                    //CmdUtil.MkLinkFile(file.FullName, linkPath, out exitCode);
                    if(hardLink)
                    {
                        Kernel32Interop.CreateHardLink(linkPath, file.FullName, IntPtr.Zero);
                    }
                    else
                    {
                        Kernel32Interop.CreateSymbolicLink(linkPath, file.FullName, Nucleus.Gaming.Platform.Windows.Interop.SymbolicLink.File);
                    }
                }
            }
        }

        public static void LinkDirectory(string root, DirectoryInfo currentDir, string destination, out int exitCode,
            string[] dirExclusions, string[] fileExclusions, string[] fileCopyInstead, bool hardLink, bool symFolders)
        {
            Console.WriteLine($"Symlinking folder {root} to {destination}");

            exitCode = 1;

            //bool special = overrideSpecial;
            bool special = false;

            if (!string.IsNullOrEmpty(dirExclusions[0]))
            {
                for (int j = 0; j < dirExclusions.Length; j++)
                {
                    string exclusion = dirExclusions[j];
                    string fullPath = Path.Combine(root, exclusion).ToLower();

                    if (!string.IsNullOrEmpty(exclusion) && fullPath.Equals(currentDir.FullName.ToLower()))
                    {
                        // special case, one of our subfolders is excluded
                        special = true;
                        break;
                    }
                }
            }


            if (!special)
            {
                // this folder has a child that cant be symlinked
                //CmdUtil.MkLinkDirectory(currentDir.FullName, destination, out exitCode);
                //Kernel32Interop.CreateSymbolicLink(destination, currentDir.FullName, Nucleus.Gaming.Platform.Windows.Interop.SymbolicLink.Directory);
                if(symFolders)
                {
                    Kernel32Interop.CreateSymbolicLink(destination, currentDir.FullName, Nucleus.Gaming.Platform.Windows.Interop.SymbolicLink.Directory);
                }
                else
                {
                    Directory.CreateDirectory(destination);
                }
                
                //CmdUtil.LinkFiles(currentDir.FullName, destination, out exitCode, fileExclusions, fileCopyInstead, true);
                WinDirectoryUtil.LinkFiles(currentDir.FullName, destination, out exitCode, fileExclusions, fileCopyInstead, hardLink);


                DirectoryInfo[] children = currentDir.GetDirectories();
                for (int i = 0; i < children.Length; i++)
                {
                    DirectoryInfo child = children[i];
                    LinkDirectory(root, child, Path.Combine(destination, child.Name), out exitCode, dirExclusions, fileExclusions, fileCopyInstead, hardLink, symFolders);
                }
            }
            else
            {
                // we symlink this directly
                //CmdUtil.MkLinkDirectory(currentDir.FullName, destination, out exitCode);

                //Kernel32Interop.CreateSymbolicLink(destination, currentDir.FullName, Nucleus.Gaming.Platform.Windows.Interop.SymbolicLink.Directory);
                //System.IO.DriveInfo di = new System.IO.DriveInfo(destination);
                //System.IO.DirectoryInfo dirInfo = di.RootDirectory;
                //Console.WriteLine("createsymboliclink: " + dirInfo.Attributes.ToString());

                if (symFolders)
                {
                    Kernel32Interop.CreateSymbolicLink(destination, currentDir.FullName, Nucleus.Gaming.Platform.Windows.Interop.SymbolicLink.Directory);
                }
                else
                {
                    Directory.CreateDirectory(destination);
                }
            }
        }
    }
}
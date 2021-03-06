#region Copyright
// 
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2016
// by DotNetNuke Corporation
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
#endregion
#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;
using DotNetNuke.UI.UserControls;
using DotNetNuke.Web.UI.WebControls;

#endregion

namespace Dnn.Modules.Languages
{

    /// <summary>
    ///   Manages translations for Resource files
    /// </summary>
    /// <returns></returns>
    /// <remarks>
    /// </remarks>
    public partial class ResourceVerifier : PortalModuleBase
    {
        #region Private Methods

        /// <summary>
        ///   Gets all system default resource files
        /// </summary>
        /// <param name = "fileList">List of found resource files</param>
        /// <param name = "path">Folder to search at</param>
        /// <remarks>
        /// </remarks>
        private static void GetResourceFiles(SortedList fileList, string path)
        {
            var folders = Directory.GetDirectories(path);
            DirectoryInfo objFolder;

            foreach (var folder in folders)
            {
                objFolder = new DirectoryInfo(folder);

                bool resxFilesDirectory = (objFolder.Name.ToLowerInvariant() == Localization.LocalResourceDirectory.ToLowerInvariant()) ||
                                          (objFolder.Name.ToLowerInvariant() == Localization.ApplicationResourceDirectory.Replace("~/","").ToLowerInvariant()) ||
                                          (folder.ToLowerInvariant().EndsWith("\\portals\\_default"));

                if (resxFilesDirectory)
                {
                    // found local resource folder, add resources

                    foreach (string file in Directory.GetFiles(objFolder.FullName, "*.resx"))
                    {
                        var fileInfo = new FileInfo(file);
                        var match = FileInfoRegex.Match(fileInfo.Name);

                        if (match.Success && match.Groups[1].Value.ToLowerInvariant() != "en-us")
                        {
                            continue;
                        }

                        fileList.Add(fileInfo.FullName, fileInfo);
                    }

                }
                else
                {
                    GetResourceFiles(fileList, folder);
                }
            }
        }

        /// <summary>
        ///   Returns the resource file name for a given locale
        /// </summary>
        /// <param name = "filename">Resource file</param>
        /// <param name = "language">Locale</param>
        /// <returns></returns>
        /// <remarks>
        /// </remarks>
        private static string ResourceFile(string filename, string language)
        {

            return Localization.GetResourceFileName(filename, language, "", Globals.GetPortalSettings().PortalId);

            //var resourcefilename = filename;

            //if (language != Localization.SystemLocale)
            //{
            //    resourcefilename = resourcefilename.Replace(".resx", "." + language + ".resx");
            //}

            //return resourcefilename;
        }

        #endregion

        #region Event Handlers

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            cmdCancel.NavigateUrl = Globals.NavigateURL();

            try
            {
                var files = new SortedList();
                Dictionary<string, Locale> locales = LocaleController.Instance.GetLocales(Null.NullInteger);
                SectionHeadControl shc;

                GetResourceFiles(files, Server.MapPath("~\\"));

                //GetResourceFiles(files, Server.MapPath("~\\admin"));
                //GetResourceFiles(files, Server.MapPath("~\\controls"));
                //GetResourceFiles(files, Server.MapPath("~\\desktopmodules"));
                //GetResourceFiles(files, Server.MapPath("~\\providers"));
                //GetResourceFiles(files, Server.MapPath("~\\install"));
                //GetResourceFiles(files, Server.MapPath("~\\Portals\\_Default\\Skins"));
                //GetResourceFiles(files, Server.MapPath("~\\App_GlobalResources"));
                // Add global and shared resource files
                //files.Add(Server.MapPath(Localization.GlobalResourceFile), new FileInfo(Server.MapPath(Localization.GlobalResourceFile)));
                //files.Add(Server.MapPath(Localization.SharedResourceFile), new FileInfo(Server.MapPath(Localization.SharedResourceFile)));


                foreach (var locale in locales.Values)
                {
                    var languageLabel = new DnnLanguageLabel {Language = locale.Code};

                    var tableTop = new HtmlTable {ID = locale.Code};
                    var rowTop = new HtmlTableRow();
                    var cellTop = new HtmlTableCell();

                    var tableMissing = new HtmlTable {ID = "Missing" + locale.Code};
                    var tableEntries = new HtmlTable {ID = "Entry" + locale.Code};
                    var tableObsolete = new HtmlTable {ID = "Obsolete" + locale.Code};
                    var tableOld = new HtmlTable {ID = "Old" + locale.Code};
                    var tableDuplicate = new HtmlTable {ID = "Duplicate" + locale.Code};
                    var tableError = new HtmlTable {ID = "Error" + locale.Code};


                    foreach (DictionaryEntry file in files)
                    {
                        // check for existance
                        if (!File.Exists(ResourceFile(file.Key.ToString(), locale.Code)))
                        {
                            var row = new HtmlTableRow();
                            var cell = new HtmlTableCell {InnerText = ResourceFile(file.Key.ToString(), locale.Code).Replace(Server.MapPath("~"), "")};
                            cell.Attributes["Class"] = "Normal";
                            row.Cells.Add(cell);
                            tableMissing.Rows.Add(row);
                        }
                        else
                        {
                            var dsDef = new DataSet();
                            var dsRes = new DataSet();
                            DataTable dtDef;
                            DataTable dtRes;

                            try
                            {
                                dsDef.ReadXml(file.Key.ToString());
                            }
                            catch
                            {
                                var row = new HtmlTableRow();
                                var cell = new HtmlTableCell {InnerText = file.Key.ToString().Replace(Server.MapPath("~"), "")};
                                cell.Attributes["Class"] = "Normal";
                                row.Cells.Add(cell);
                                tableError.Rows.Add(row);
                                dsDef = null;
                            }
                            try
                            {
                                dsRes.ReadXml(ResourceFile(file.Key.ToString(), locale.Code));
                            }
                            catch
                            {
                                if (locale.Text != Localization.SystemLocale)
                                {
                                    var row = new HtmlTableRow();
                                    var cell = new HtmlTableCell {InnerText = ResourceFile(file.Key.ToString(), locale.Code).Replace(Server.MapPath("~"), "")};
                                    cell.Attributes["Class"] = "Normal";
                                    row.Cells.Add(cell);
                                    tableError.Rows.Add(row);
                                    dsRes = null;
                                }
                            }

                            if (dsRes != null && dsDef != null && dsRes.Tables["data"] != null && dsDef.Tables["data"] != null)
                            {
                                dtDef = dsDef.Tables["data"];
                                dtDef.TableName = "default";
                                dtRes = dsRes.Tables["data"].Copy();
                                dtRes.TableName = "localized";
                                dsDef.Tables.Add(dtRes);

                                // Check for duplicate entries in localized file
                                try
                                {
                                    // if this fails-> file contains duplicates
                                    var c = new UniqueConstraint("uniqueness", dtRes.Columns["name"]);
                                    dtRes.Constraints.Add(c);
                                    dtRes.Constraints.Remove("uniqueness");
                                }
                                catch
                                {
                                    var row = new HtmlTableRow();
                                    var cell = new HtmlTableCell {InnerText = ResourceFile(file.Key.ToString(), locale.Code).Replace(Server.MapPath("~"), "")};
                                    cell.Attributes["Class"] = "Normal";
                                    row.Cells.Add(cell);
                                    tableDuplicate.Rows.Add(row);
                                }

                                // Check for missing entries in localized file
                                try
                                {
                                    // if this fails-> some entries in System default file are not found in Resource file
                                    dsDef.Relations.Add("missing", dtRes.Columns["name"], dtDef.Columns["name"]);
                                }
                                catch
                                {
                                    var row = new HtmlTableRow();
                                    var cell = new HtmlTableCell {InnerText = ResourceFile(file.Key.ToString(), locale.Code).Replace(Server.MapPath("~"), "")};
                                    cell.Attributes["Class"] = "Normal";
                                    row.Cells.Add(cell);
                                    tableEntries.Rows.Add(row);
                                }
                                finally
                                {
                                    dsDef.Relations.Remove("missing");
                                }

                                // Check for obsolete entries in localized file
                                try
                                {
                                    // if this fails-> some entries in Resource File are not found in System default
                                    dsDef.Relations.Add("obsolete", dtDef.Columns["name"], dtRes.Columns["name"]);
                                }
                                catch
                                {
                                    var row = new HtmlTableRow();
                                    var cell = new HtmlTableCell {InnerText = ResourceFile(file.Key.ToString(), locale.Code).Replace(Server.MapPath("~"), "")};
                                    cell.Attributes["Class"] = "Normal";
                                    row.Cells.Add(cell);
                                    tableObsolete.Rows.Add(row);
                                }
                                finally
                                {
                                    dsDef.Relations.Remove("obsolete");
                                }

                                // Check older files
                                var resFile = new FileInfo(ResourceFile(file.Key.ToString(), locale.Code));
                                if (((FileInfo) file.Value).LastWriteTime > resFile.LastWriteTime)
                                {
                                    var row = new HtmlTableRow();
                                    var cell = new HtmlTableCell {InnerText = ResourceFile(file.Key.ToString(), locale.Code).Replace(Server.MapPath("~"), "")};
                                    cell.Attributes["Class"] = "Normal";
                                    row.Cells.Add(cell);
                                    tableOld.Rows.Add(row);
                                }
                            }
                        }
                    }

                    if (tableMissing.Rows.Count > 0)
                    {
                        // ------- Missing files
                        shc = (SectionHeadControl) LoadControl("~/controls/sectionheadcontrol.ascx");
                        shc.Section = "Missing" + locale.Code;
                        shc.IncludeRule = false;
                        shc.IsExpanded = false;
                        shc.CssClass = "SubHead";
                        shc.Text = Localization.GetString("MissingFiles", LocalResourceFile) + tableMissing.Rows.Count;
                        cellTop.Controls.Add(shc);
                        cellTop.Controls.Add(tableMissing);
                    }

                    if (tableDuplicate.Rows.Count > 0)
                    {
                        // ------- Duplicate keys
                        shc = (SectionHeadControl) LoadControl("~/controls/sectionheadcontrol.ascx");
                        shc.Section = "Duplicate" + locale.Code;
                        shc.IncludeRule = false;
                        shc.IsExpanded = false;
                        shc.CssClass = "SubHead";
                        shc.Text = Localization.GetString("DuplicateEntries", LocalResourceFile) + tableDuplicate.Rows.Count;
                        cellTop.Controls.Add(shc);
                        cellTop.Controls.Add(tableDuplicate);
                    }

                    if (tableEntries.Rows.Count > 0)
                    {
                        // ------- Missing entries
                        shc = (SectionHeadControl) LoadControl("~/controls/sectionheadcontrol.ascx");
                        shc.Section = "Entry" + locale.Code;
                        shc.IncludeRule = false;
                        shc.IsExpanded = false;
                        shc.CssClass = "SubHead";
                        shc.Text = Localization.GetString("MissingEntries", LocalResourceFile) + tableEntries.Rows.Count;
                        cellTop.Controls.Add(shc);
                        cellTop.Controls.Add(tableEntries);
                    }

                    if (tableObsolete.Rows.Count > 0)
                    {
                        // ------- Missing entries
                        shc = (SectionHeadControl) LoadControl("~/controls/sectionheadcontrol.ascx");
                        shc.Section = "Obsolete" + locale.Code;
                        shc.IncludeRule = false;
                        shc.IsExpanded = false;
                        shc.CssClass = "SubHead";
                        shc.Text = Localization.GetString("ObsoleteEntries", LocalResourceFile) + tableObsolete.Rows.Count;
                        cellTop.Controls.Add(shc);
                        cellTop.Controls.Add(tableObsolete);
                    }

                    if (tableOld.Rows.Count > 0)
                    {
                        // ------- Old files
                        shc = (SectionHeadControl) LoadControl("~/controls/sectionheadcontrol.ascx");
                        shc.Section = "Old" + locale.Code;
                        shc.IncludeRule = false;
                        shc.IsExpanded = false;
                        shc.CssClass = "SubHead";
                        shc.Text = Localization.GetString("OldFiles", LocalResourceFile) + tableOld.Rows.Count;
                        cellTop.Controls.Add(shc);
                        cellTop.Controls.Add(tableOld);
                    }

                    if (tableError.Rows.Count > 0)
                    {
                        // ------- Error files
                        shc = (SectionHeadControl) LoadControl("~/controls/sectionheadcontrol.ascx");
                        shc.Section = "Error" + locale.Code;
                        shc.IncludeRule = false;
                        shc.IsExpanded = false;
                        shc.CssClass = "SubHead";
                        shc.Text = Localization.GetString("ErrorFiles", LocalResourceFile) + tableError.Rows.Count;
                        cellTop.Controls.Add(shc);
                        cellTop.Controls.Add(tableError);
                    }

                    rowTop.Cells.Add(cellTop);
                    tableTop.Rows.Add(rowTop);

                    PlaceHolder1.Controls.Add(languageLabel);
                    PlaceHolder1.Controls.Add(tableTop);
                    PlaceHolder1.Controls.Add(new LiteralControl("<br>"));
                }
                //Module failed to load
            }
            catch (Exception exc)
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        #endregion

    }
}

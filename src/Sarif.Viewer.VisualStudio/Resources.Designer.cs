﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Sarif.Viewer {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Sarif.Viewer.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Done processing SARIF log &apos;{0}&apos; is complete..
        /// </summary>
        public static string CompletedProcessingLogFileFormat {
            get {
                return ResourceManager.GetString("CompletedProcessingLogFileFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Always allow downloads from &apos;{0}&apos;.
        /// </summary>
        public static string ConfirmDownloadDialog_CheckboxLabel {
            get {
                return ResourceManager.GetString("ConfirmDownloadDialog_CheckboxLabel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Do you want to download the source file from this location?
        ///
        ///{0}.
        /// </summary>
        public static string ConfirmDownloadDialog_Message {
            get {
                return ResourceManager.GetString("ConfirmDownloadDialog_Message", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Confirm Download.
        /// </summary>
        public static string ConfirmDownloadDialog_Title {
            get {
                return ResourceManager.GetString("ConfirmDownloadDialog_Title", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Continuing.
        /// </summary>
        public static string ContinuingCallTreeNodeMessage {
            get {
                return ResourceManager.GetString("ContinuingCallTreeNodeMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The log file you have opened will be converted to SARIF. Would you like to save the converted file?.
        /// </summary>
        public static string ConvertNonSarifLog_DialogMessage {
            get {
                return ResourceManager.GetString("ConvertNonSarifLog_DialogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred downloading the source file..
        /// </summary>
        public static string DownloadFail_DialogMessage {
            get {
                return ResourceManager.GetString("DownloadFail_DialogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The InfoBar for condition &apos;{0}&apos; is already present in the dictionary..
        /// </summary>
        public static string ErrorInfoBarAlreadyPresent {
            get {
                return ResourceManager.GetString("ErrorInfoBarAlreadyPresent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The SARIF viewer received one or more corrupted SARIF logs, and they were ignored..
        /// </summary>
        public static string ErrorInvalidSarifStream {
            get {
                return ResourceManager.GetString("ErrorInvalidSarifStream", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SARIF Viewer: Absent results &quot;stub&quot; display source.
        /// </summary>
        public static string ErrorListAbsentResultsDataSourceDisplayName {
            get {
                return ResourceManager.GetString("ErrorListAbsentResultsDataSourceDisplayName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SARIF Viewer: Suppressed results &quot;stub&quot; display source.
        /// </summary>
        public static string ErrorListSuppressedResultsDataSourceDisplayName {
            get {
                return ResourceManager.GetString("ErrorListSuppressedResultsDataSourceDisplayName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SARIF Viewer.
        /// </summary>
        public static string ErrorListTableDataSourceDisplayName {
            get {
                return ResourceManager.GetString("ErrorListTableDataSourceDisplayName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The SARIF viewer received one or more SARIF logs containing a run that failed due to a tool configuration error. These logs might contain incomplete results..
        /// </summary>
        public static string ErrorLogHasErrorLevelToolConfigurationNotifications {
            get {
                return ResourceManager.GetString("ErrorLogHasErrorLevelToolConfigurationNotifications", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The SARIF viewer received one or more SARIF logs containing a run that failed due to a tool execution error. These logs might contain incomplete results..
        /// </summary>
        public static string ErrorLogHasErrorLevelToolExecutionNotifications {
            get {
                return ResourceManager.GetString("ErrorLogHasErrorLevelToolExecutionNotifications", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to false positive.
        /// </summary>
        public static string FalsePositiveResult {
            get {
                return ResourceManager.GetString("FalsePositiveResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Open File.
        /// </summary>
        public static string FileOpenFail_DialogCaption {
            get {
                return ResourceManager.GetString("FileOpenFail_DialogCaption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file &apos;{0}&apos; couldn&apos;t be opened by Visual Studio. Would you like to open the containing folder?.
        /// </summary>
        public static string FileOpenFail_DialogMessage {
            get {
                return ResourceManager.GetString("FileOpenFail_DialogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Original.
        /// </summary>
        public static string FixPreviewWindow_OriginalFileTitle {
            get {
                return ResourceManager.GetString("FixPreviewWindow_OriginalFileTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fix Preview.
        /// </summary>
        public static string FixPreviewWindow_PreviewFixedFileTitle {
            get {
                return ResourceManager.GetString("FixPreviewWindow_PreviewFixedFileTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Android Studio log files (*.xml)|*.xml.
        /// </summary>
        public static string ImportAndroidStudioFilter {
            get {
                return ResourceManager.GetString("ImportAndroidStudioFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Clang log files (*.xml)|*.xml.
        /// </summary>
        public static string ImportClangAnalyzerFilter {
            get {
                return ResourceManager.GetString("ImportClangAnalyzerFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Contrast Security files (*.xml)|*.xml.
        /// </summary>
        public static string ImportContrastSecurityFilter {
            get {
                return ResourceManager.GetString("ImportContrastSecurityFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CppCheck log files (*.xml)|*.xml.
        /// </summary>
        public static string ImportCppCheckFilter {
            get {
                return ResourceManager.GetString("ImportCppCheckFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FlawFinder log files (*.csv)|*.csv.
        /// </summary>
        public static string ImportFlawFinderFilter {
            get {
                return ResourceManager.GetString("ImportFlawFinderFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fortify log files (*.xml)|*.xml.
        /// </summary>
        public static string ImportFortifyFilter {
            get {
                return ResourceManager.GetString("ImportFortifyFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fortify FPR log files (*.fpr)|*.fpr.
        /// </summary>
        public static string ImportFortifyFprFilter {
            get {
                return ResourceManager.GetString("ImportFortifyFprFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FxCop report and project files (*.xml)|*.xml.
        /// </summary>
        public static string ImportFxCopFilter {
            get {
                return ResourceManager.GetString("ImportFxCopFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Import analysis log.
        /// </summary>
        public static string ImportLogOpenFileDialogTitle {
            get {
                return ResourceManager.GetString("ImportLogOpenFileDialogTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MSBuild files (*.*)|*.*.
        /// </summary>
        public static string ImportMSBuildFilter {
            get {
                return ResourceManager.GetString("ImportMSBuildFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SARIF files (*.sarif)|*.sarif.
        /// </summary>
        public static string ImportNoneFilter {
            get {
                return ResourceManager.GetString("ImportNoneFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to PREfast log files (*.xml)|*.xml.
        /// </summary>
        public static string ImportPREfastFilter {
            get {
                return ResourceManager.GetString("ImportPREfastFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Pylint log files (*.json)|*.json.
        /// </summary>
        public static string ImportPylintFilter {
            get {
                return ResourceManager.GetString("ImportPylintFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Semmle QL log files (*.csv)|*.csv.
        /// </summary>
        public static string ImportSemmleQLFilter {
            get {
                return ResourceManager.GetString("ImportSemmleQLFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Static Driver Verifier log files (*.tt)|*.tt.
        /// </summary>
        public static string ImportStaticDriverVerifierFilter {
            get {
                return ResourceManager.GetString("ImportStaticDriverVerifierFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TSLint log files (*.json)|*.json.
        /// </summary>
        public static string ImportTSLintFilter {
            get {
                return ResourceManager.GetString("ImportTSLintFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The SARIF viewer received one or more SARIF logs in which the analysis tool did not detect any results..
        /// </summary>
        public static string InfoNoResultsInLog {
            get {
                return ResourceManager.GetString("InfoNoResultsInLog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The log file &apos;{0}&apos; is invalid and couldn&apos;t be opened..
        /// </summary>
        public static string LogOpenFail_InvalidFormat_DialogMessage {
            get {
                return ResourceManager.GetString("LogOpenFail_InvalidFormat_DialogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to low value.
        /// </summary>
        public static string LowValueResult {
            get {
                return ResourceManager.GetString("LowValueResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to non-actionable.
        /// </summary>
        public static string NonActionableResult {
            get {
                return ResourceManager.GetString("NonActionableResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to non-shipping code.
        /// </summary>
        public static string NonShippingCodeResult {
            get {
                return ResourceManager.GetString("NonShippingCodeResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The log file &apos;{0}&apos; was not found..
        /// </summary>
        public static string OpenLogFileFail_DialogMessage {
            get {
                return ResourceManager.GetString("OpenLogFileFail_DialogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to other.
        /// </summary>
        public static string OtherResult {
            get {
                return ResourceManager.GetString("OtherResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Processing SARIF log &apos;{0}&apos;….
        /// </summary>
        public static string ProcessingLogFileFormat {
            get {
                return ResourceManager.GetString("ProcessingLogFileFormat", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Processing SARIF logs….
        /// </summary>
        public static string ProcessLogFiles {
            get {
                return ResourceManager.GetString("ProcessLogFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Completed processing SARIF logs..
        /// </summary>
        public static string ProcessLogFilesComplete {
            get {
                return ResourceManager.GetString("ProcessLogFilesComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Report {0} result: {1}.
        /// </summary>
        public static string ReportResultTitle {
            get {
                return ResourceManager.GetString("ReportResultTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Return.
        /// </summary>
        public static string ReturnMessage {
            get {
                return ResourceManager.GetString("ReturnMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        public static byte[] RuleLookup {
            get {
                object obj = ResourceManager.GetObject("RuleLookup", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SARIF Explorer.
        /// </summary>
        public static string SarifExplorerCaption {
            get {
                return ResourceManager.GetString("SarifExplorerCaption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save Converted Log File.
        /// </summary>
        public static string SaveConvertedLog_DialogTitle {
            get {
                return ResourceManager.GetString("SaveConvertedLog_DialogTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SARIF log files (*.sarif)|*.sarif.
        /// </summary>
        public static string SaveDialogFileFilter {
            get {
                return ResourceManager.GetString("SaveDialogFileFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The log file couldn&apos;t be saved because access to the path &apos;{0}&apos; was denied..
        /// </summary>
        public static string SaveLogFail_Access_DialogMessage {
            get {
                return ResourceManager.GetString("SaveLogFail_Access_DialogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The log file couldn&apos;t be saved: {0}.
        /// </summary>
        public static string SaveLogFail_General_Dialog {
            get {
                return ResourceManager.GetString("SaveLogFail_General_Dialog", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save Transformed Log File.
        /// </summary>
        public static string SaveTransformedPrereleaseV2Log_DialogTitle {
            get {
                return ResourceManager.GetString("SaveTransformedPrereleaseV2Log_DialogTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save Transformed Log File.
        /// </summary>
        public static string SaveTransformedV1Log_DialogTitle {
            get {
                return ResourceManager.GetString("SaveTransformedV1Log_DialogTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The log file you have opened conforms to a pre-release SARIF version 2 schema. This file will be automatically transformed to SARIF version {0}. Would you like to save the transformed file?.
        /// </summary>
        public static string TransformPrereleaseV2_DialogMessage {
            get {
                return ResourceManager.GetString("TransformPrereleaseV2_DialogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The log file you have opened is SARIF version 1. This file will be automatically transformed to SARIF version 2. Would you like to save the transformed file?.
        /// </summary>
        public static string TransformV1_DialogMessage {
            get {
                return ResourceManager.GetString("TransformV1_DialogMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;unknown callee&gt;.
        /// </summary>
        public static string UnknownCalleeMessage {
            get {
                return ResourceManager.GetString("UnknownCalleeMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown.
        /// </summary>
        public static string UnknownToolName {
            get {
                return ResourceManager.GetString("UnknownToolName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The SARIF specification recommends that the version property appear at the beginning of the log file. This log file does not conform to that recommendation..
        /// </summary>
        public static string VersionPropertyNotFound_DialogTitle {
            get {
                return ResourceManager.GetString("VersionPropertyNotFound_DialogTitle", resourceCulture);
            }
        }
    }
}

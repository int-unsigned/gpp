using System.Diagnostics;


namespace gpp.builder
{
  internal class BuilderCommandLine
  {
    public class BuilderCommandLineAppSite() : AppSite
    {
      private readonly AppNotify  m_Notify  = new AppNotifyDummy();
      private readonly AppLog     m_Log     = new AppLog();
      //
      public AppNotify Notify => m_Notify;
      public AppLog Log       => m_Log;
      public AppSite CreateAppSiteForThread()
      {
        return new BuilderCommandLineAppSite();
      }
      public void CombineLog(params AppLog[] other_logs_)
      {
        m_Log.Append(other_logs_);
      }
      //
    }


    const int EXITCODE_S_OK      = 0;
    const int EXITCODE_S_FALSE   = 0;
    const int EXITCODE_E_FAIL    = 1;

    const string HELP_INFORMATION =
@"Usage: goldbuild [path]grammar-file-name[.grm] [egt-file-path-name.egt] [-option]
  Source grammar-file-name mandatory.
    If path not specified try search in current folder
    If extension not specified try search with .grm extension
  Output grammar-tables file (egt) optional. 
    If not specified constructed from source file name and it folder plus .egt extension.
    If specified must be full path name with extension
  Option:
    -d:, -dat:""path""      path to GOLD Parser configuration files (gp.dat, etc..). 
                          (must be in quarter (-d:""path"") without trailing slash)
    -l,  -log             use log file <output-grammar-tables-file>.log
    -l:, -log:""file""      use this log file
    -w,  -warning         don`t show warning to console
    -n,  -nologo          don`t  display start logo
    -v,  -verbose         detail output
    -h,  -help            display this help and exit
";


    static string?          s_log_file  = null;
    static TextWriter       m_output    = Console.Out;

    static int Main(string[] args_)
    {
      if (args_.Length == 0)
        return ReportLogoAndHelp();

      string?   s_grammar_file    = null;
      string?   s_result_file     = null;
      string?   s_dat_path        = null;
      string?   m_gen_date_text   = null;
      DateTime  m_gen_date_date   = default;
      bool      b_opt_logo        = true;
      bool      b_opt_verbose     = false;
      bool      b_opt_no_warning  = false;
      //
      for (int i_arg = 0; i_arg < args_.Length; ++i_arg)
      { 
        string s_arg = args_[i_arg];        
        if (s_arg[0] == '-')
        {
          string s_arg_opt = s_arg.Substring(1);
          if (s_arg_opt.StartsWith("dat:"))
            s_dat_path = s_arg_opt.Substring(4);
          else if (s_arg_opt.StartsWith("d:"))
            s_dat_path = s_arg_opt.Substring(2);
          else if (s_arg_opt.StartsWith("g:"))
          {
            m_gen_date_text = s_arg_opt.Substring(2);
            if (!DateTime.TryParse(m_gen_date_text, out m_gen_date_date))
              return ReportCritical("Invalid GenerateDate option", m_gen_date_text);
          }
          else if (s_arg_opt.StartsWith("log:"))
            s_log_file = s_arg_opt.Substring(4);
          else if (s_arg_opt.StartsWith("l:"))
            s_log_file = s_arg_opt.Substring(2);
          else
          {
            switch (s_arg_opt)
            {
              case "nologo":
              case "n":
                b_opt_logo = false;
                break;
              case "help":
              case "h":
                return ReportLogoAndHelp();
              case "verbose":
              case "v":
                b_opt_verbose = true;
                break;
              case "warning":
              case "w":
                b_opt_no_warning = true;
                break;
              case "log":
              case "l":
                s_log_file = string.Empty;
                break;
              default:
                return ReportCritical("Unexpected command line option", s_arg_opt);
            }
          }
        }
        else if (s_grammar_file == null)
          s_grammar_file = s_arg;
        else if (s_result_file == null)
          s_result_file = s_arg;
        else 
          return ReportCritical("Unexpected command line argument", s_arg);
      }

      if (b_opt_logo)
        Console.WriteLine(GOLDParserInfo.Logo);

      if (s_dat_path == null)
        s_dat_path = AppDatDir();

      if (s_grammar_file == null)
        return ReportCritical($"Grammar file not specified");

      if (b_opt_verbose)
        ReportInformation($"Try found grammar file", s_grammar_file);
      //
      string  s_grammar_file_path_name      = Path.GetFullPath(s_grammar_file);
      string? s_grammar_file_path_name_ext  = null;
      if (File.Exists(s_grammar_file_path_name))
        s_grammar_file_path_name_ext = s_grammar_file_path_name;
      else 
      {
        string s_file = s_grammar_file_path_name += ".grm";
        if (File.Exists(s_file))
          s_grammar_file_path_name_ext = s_file;
      }
      //
      if (s_grammar_file_path_name_ext == null) 
        return ReportCritical("Grammar file not found", s_grammar_file_path_name);
      else if (b_opt_verbose)
        ReportInformation($"Use grammar file", s_grammar_file_path_name);


      string s_result_file_path_name_ext;
      if (s_result_file == null)
      {
        string? s_result_file_path = Path.GetDirectoryName(s_grammar_file_path_name_ext);
        if (s_result_file_path == null)
          return ReportCritical("Can`t construct result file path from grammar file", s_grammar_file_path_name_ext);

        string? s_result_file_name = Path.GetFileNameWithoutExtension(s_grammar_file_path_name_ext);
        if (s_result_file_name == null)
          return ReportCritical("Can`t construct result file name from grammar file", s_grammar_file_path_name_ext);

        string s_result_file_path_name = Path.Combine(s_result_file_path, s_result_file_name);
        s_result_file_path_name_ext = s_result_file_path_name + ".egt";
      }
      else 
        s_result_file_path_name_ext = s_result_file;

      if(b_opt_verbose)
        ReportInformation($"Will use result file", s_result_file_path_name_ext);


      if (s_log_file != null)
      {
        if (s_log_file.Empty())
        {
          string s_result_path = Path.GetDirectoryName(s_result_file_path_name_ext)!;
          string s_result_name = Path.GetFileNameWithoutExtension(s_result_file_path_name_ext)!;
          s_log_file = Path.Join(s_result_path, s_result_name + ".log");
        }
        try { 
          StreamWriter output_stream = File.CreateText(s_log_file);
          output_stream.AutoFlush = true;
          m_output = output_stream;
        }
        catch (Exception ex)  { 
          return ReportCritical($"Can`t open log file '{s_log_file}'", ex.Message); 
        }

        if (b_opt_verbose) 
        { // duplicate information to log file
          ReportInformation($"Use grammar file", s_grammar_file_path_name);
          ReportInformation($"Will use result file", s_result_file_path_name_ext);
          Console.WriteLine($"Detail: Will use log file: '{s_log_file}'");
        }          
      }


      string s_gpp_dat_grm_file_path_name = AppDataGrammarFilePathName(s_dat_path);
      if (!File.Exists(s_gpp_dat_grm_file_path_name))
        return ReportCritical("Not found parser data file", s_gpp_dat_grm_file_path_name);
      else if (b_opt_verbose)
        ReportInformation($"Use parser data file", s_gpp_dat_grm_file_path_name);

      string s_gpp_dat_set_file_path_name = AppDataSetsFilePathName(s_dat_path);
      if (!File.Exists(s_gpp_dat_set_file_path_name))
        return ReportCritical("Not found predefined charsets file", s_gpp_dat_set_file_path_name);
      else if (b_opt_verbose)
        ReportInformation($"Use predefined charsets file", s_gpp_dat_set_file_path_name);

      string s_gpp_dat_map_file_path_name = AppDataMappingFilePathName(s_dat_path);
      if (!File.Exists(s_gpp_dat_map_file_path_name))
        return ReportCritical("Not found unicode mapping file", s_gpp_dat_map_file_path_name);
      else if (b_opt_verbose)
        ReportInformation($"Use unicode mapping file", s_gpp_dat_map_file_path_name);


      BuilderCommandLineAppSite MyAppSite = new();


      Builder? builder = Builder.Create(MyAppSite.Log, s_gpp_dat_grm_file_path_name, s_gpp_dat_set_file_path_name, s_gpp_dat_map_file_path_name);
      if (builder == null)
        return ReportCriticalLog(MyAppSite.Log);
      else if (b_opt_verbose)
        ReportInformation($"Create Builder Oк");


      string s_grammar_file_content = string.Empty;
      using (TextReader grammar_reader = (TextReader)new StreamReader(s_grammar_file_path_name_ext, detectEncodingFromByteOrderMarks: true))
      {
        string? s_grammar_line = null;
        do
        {
          s_grammar_line = grammar_reader.ReadLine();
          if (s_grammar_line != null)
            s_grammar_file_content += (s_grammar_line + "\r\n");
        }
        while (s_grammar_line != null);
        //
        grammar_reader.Close();
      }


      TextReader s_grammar_file_reader = new StringReader(s_grammar_file_content);
      (bool b_parse_grammar_ok, GrammarTables grammar_tables) = builder.ParseGrammar(MyAppSite, s_grammar_file_reader);
      if (!b_parse_grammar_ok)
        return ReportCriticalLog(MyAppSite.Log);
      else if (b_opt_verbose)
        ReportInformation($"Parse grammar Oк");


      (bool b_grammar_analize_ok, BuilderTables builder_tables, BuilderSymbol? start_symbol) = BuilderEngine.ConstructBuilderTables(MyAppSite, builder, grammar_tables);
      if (!b_grammar_analize_ok || start_symbol == null)
        return ReportCriticalLog(MyAppSite.Log);
      else if (b_opt_verbose)
        ReportInformation($"Build grammar tables Oк");


      BuilderEngine builder_engine = new BuilderEngine();
      (bool b_lr_ok, BuilderLRStatesList lr_states, bool b_fa_ok, BuilderFACharsetsList fa_charsets, BuilderFAStatesList fa_states) = builder_engine.ComputeUseTask(MyAppSite, builder_tables, start_symbol);
      if (!b_lr_ok || !b_fa_ok)
        return ReportCriticalLog(MyAppSite.Log);
      else if (b_opt_verbose)
        ReportInformation("Compute grammar Oк");


      if(m_gen_date_text != null)
        BuilderEngine.ComputeComplete(builder_tables, m_gen_date_date);
      else
        BuilderEngine.ComputeComplete(builder_tables);


      bool b_store_ok = BuilderTablesStorer.BuilderTableSaveVer5(MyAppSite.Log, s_result_file_path_name_ext, builder_tables, lr_states, fa_charsets, fa_states);
      if (!b_store_ok)
        return ReportCriticalLog(MyAppSite.Log);


      bool use_log_file = !ReferenceEquals(m_output, Console.Out);
      ReportDoneLog(MyAppSite.Log, use_log_file, b_opt_no_warning);

      //NOTE  Warnings is Success only if explicity defined log-file and option no-warning      
      if (MyAppSite.Log.LoggedWarning() && (use_log_file == false || b_opt_no_warning == false))
        Console.WriteLine($"Complete with warning: {s_result_file_path_name_ext}");
      else
        Console.WriteLine($"Success: {s_result_file_path_name_ext}");
     
      return EXITCODE_S_OK;
    } //main



    private static int ReportLogoAndHelp()
    {
      Console.WriteLine(gpp.GppGlobal.GOLDParserInfo.Logo);
      Console.WriteLine(HELP_INFORMATION);
      return EXITCODE_S_FALSE;
    }

    private static void ReportDoneLog(AppLog log_, bool use_log_file_, bool opt_no_warning_)
    {      
      bool has_critical = log_.LoggedCriticalError();
      bool has_warning  = log_.LoggedWarning();

      // don't show warning only on screen, only if no crirical, and option -w is set
      bool show_warning = has_critical || opt_no_warning_ == false || use_log_file_;

      ReportLog(log_, show_warning);
    }

    private static int ReportCriticalLog(AppLog log_)
    {
      Debug.Assert(log_.LoggedCriticalError());
      if (!log_.LoggedCriticalError())
        return ReportCritical("Internal Error - no LoggedCriticalError");

      ReportLog(log_, true);

      return EXITCODE_E_FAIL;
    }


    private static void ReportLog(AppLog log_, bool show_warning_)
    {
      for (int i = 0; i < log_.Count(); ++i)
      {
        AppLogItem log_item = log_[i];
        if (log_item.Alert == AppLogAlert.Critical)
          ReportLogItemCritical(log_item);
        else if (show_warning_ && log_item.Alert == AppLogAlert.Warning)
          ReportLogItemWarning(log_item);
      }
    }
    private static void ReportLogItemWarning(AppLogItem log_item_)      =>      ReportWarning(log_item_.Title);
    private static void ReportLogItemCritical(AppLogItem log_item_)     =>      ReportCritical(log_item_.Title);
    private static void ReportInformation(string text_)                 =>      Report($"Detail: {text_}");
    private static void ReportInformation(string text_, string option_) =>      ReportInformation($"{text_}: '{option_}'");
    private static void ReportWarning(string text_)                     =>      Report($"WARNING: {text_}");
    private static int ReportCritical(string text_, string option_)     =>      ReportCritical($"{text_}: '{option_}'");
    private static int ReportCritical(string text_)
    {
      Report($"ERROR: {text_}");
      return EXITCODE_E_FAIL;
    }


    private static void Report(string text_)
    {
      m_output.WriteLine(text_);
    }

  }
}
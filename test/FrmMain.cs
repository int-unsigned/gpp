//
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

using System.Text.RegularExpressions;

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;


//
using gpp.builder;
using static System.Windows.Forms.LinkLabel;



namespace gpp_maker
{

  public partial class FrmMain : Form
  {
    Stopwatch m_tm        = new Stopwatch();
    Stopwatch m_tm_all    = new Stopwatch();
    string    m_test_log  = string.Empty;

    public FrmMain()
    {
      InitializeComponent();
    }

    static void debug_write_array(string name_, byte[] array_, int len_)
    {
      Debug.Write(name_ + ": [");
      if (len_ > 0)
      {
        Debug.Write(array_[0]);
        for (int i = 0; i < len_; ++i)
        {
          Debug.Write(", ");
          Debug.Write(array_[i]);
        }
      }
      Debug.WriteLine("]");
    }


    static bool IsFilesEqual_Hash(FileInfo first_file_, byte[] second_hash_)
    {
      //debug_write_array("second_hash_", second_hash_, MD5.HashSizeInBytes);

      byte[] first_hash = MD5.Create().ComputeHash(first_file_.OpenRead());
      for (int i = 0; i < first_hash.Length; i++)
      {
        if (first_hash[i] != second_hash_[i])
          return false;
      }
      return true;
    }
    static bool IsFilesEqual_Hash(FileInfo first, FileInfo second)
    {
      return IsFilesEqual_Hash(first, MD5.HashData(second.OpenRead()));
    }
    static bool IsFilesEqual(string first_path_name_, string second_path_name_)
    {
      return IsFilesEqual_Hash(new FileInfo(first_path_name_), new FileInfo(second_path_name_));
    }
    //static bool IsFileEqualOriginal(string egt_file_path_name_)
    //{
    //  string sEgtOriginal_FilePathName = "D:\\dev\\gpp\\gpp_test\\v8x3.egt_original";
    //  byte[] sEgtOriginal_Hash = new byte[] { 44, 193, 70, 236, 125, 205, 56, 205, 228, 95, 67, 123, 67, 42, 222, 80 };
    //  return IsFilesEqual_Hash(new FileInfo(egt_file_path_name_), sEgtOriginal_Hash);
    //}

    void act_start()
    {
      m_tm.Start();
    }
    void act_done([System.Runtime.CompilerServices.CallerMemberName] string member_name_ = "")
    {
      m_tm.Stop();
        TimeSpan tm = m_tm.Elapsed;
      m_tm.Reset();
      string s = member_name_ + ":";
      int lNameMax = 30;  // "Do_ComputeComplete___:".Length;
      s = s.PadRight(lNameMax, ' ');
      m_test_log += (s + tm.ToString() + Environment.NewLine);
    }


    Builder? Do_BuilserSetup(AppSite app_)
    {
      act_start();
        var B = Builder.Create(app_.Log, AppDataGrammarFilePathName(), AppDataSetsFilePathName(), AppDataMappingFilePathName());
      act_done();
      return B;
    }
    string Do_GrammarRead(string grammar_file_path_name_)
    {
      act_start();
      //
      string s_grammar = "";
      using (TextReader grammar_reader = (TextReader)new StreamReader(grammar_file_path_name_, detectEncodingFromByteOrderMarks: true))
      {
        string? s_grammar_line = null;
        do
        {
          s_grammar_line = grammar_reader.ReadLine();
          if (s_grammar_line != null)
            s_grammar = s_grammar + s_grammar_line + "\r\n";
        }
        while (s_grammar_line != null);
        //
        grammar_reader.Close();
      }
      //
      act_done();

      return s_grammar;
    }

    (bool, GrammarTables) Do_ConstructGrammarTables(AppSite app_, Builder builder_, string s_grammar_)
    {
      act_start();
        TextReader user_grammar_source = (TextReader)new StringReader(s_grammar_);
        (bool b_parse_grammar_ok, GrammarTables grammar_tables) = builder_.ParseGrammar(app_, user_grammar_source);
      act_done();
      return (b_parse_grammar_ok, grammar_tables);
    }
    (bool, BuilderTables, BuilderSymbol? start_symbol) Do_ConstructBuilderTables(AppSite app_, Builder builder_, GrammarTables grammar_tables_)
    {
      act_start();
        var builder_tables_construct_result = BuilderEngine.ConstructBuilderTables(app_, builder_, grammar_tables_);
      act_done();
      return builder_tables_construct_result;
    }
    (bool, BuilderLRStatesList, bool, BuilderFACharsetsList, BuilderFAStatesList) Do_ComputeGrammarUseSync(AppSite app_, BuilderTables builder_tables_, BuilderSymbol start_symbol_)
    {
      act_start();
        BuilderEngine builder_engine = new BuilderEngine();
        var result = builder_engine.ComputeUseSync(app_, builder_tables_, start_symbol_);
      act_done();
      return result;
    }
    (bool, BuilderLRStatesList, bool, BuilderFACharsetsList, BuilderFAStatesList) Do_ComputeGrammarUseTask(AppSite app_, BuilderTables builder_tables_, BuilderSymbol start_symbol_)
    {
      act_start();
        BuilderEngine builder_engine = new BuilderEngine();
        var result = builder_engine.ComputeUseTask(app_, builder_tables_, start_symbol_);
      act_done();
      return result;
    }
    void Do_ComputeComplete(BuilderTables builder_tables_)
    {
      act_start();
        BuilderEngine.ComputeComplete(builder_tables_);
      act_done();
    }
    bool Do_SaveVer5(AppSite app_, string egt_file_path_name_, BuilderTables builder_tables_, BuilderLRStatesList this_LALR_, BuilderFACharsetsList this_CharSet_, BuilderFAStatesList this_DFA_)
    {
      act_start();
        bool bOk = BuilderTablesStorer.BuilderTableSaveVer5(app_.Log, egt_file_path_name_, builder_tables_, this_LALR_, this_CharSet_, this_DFA_);
      act_done();
      return bOk;
    }

    private readonly string _TestFilesPath = Path.Join(AppDir(), "test_files");

    private string make_file_path_name_grm(string name_) => Path.Combine(_TestFilesPath, name_ + ".grm");
    private string make_file_path_name_egt(string name_) => Path.Combine(_TestFilesPath, name_ + ".egt");
    private string make_file_path_name_egt_original(string name_) => Path.Combine(_TestFilesPath, name_ + ".egt_original");


    private void test_cfg_save()
    {
      string test_cfg_file_path_name = Path.Combine(_TestFilesPath, "test.cfg");
      if (File.Exists(test_cfg_file_path_name)) 
        File.Delete(test_cfg_file_path_name);

      using (StreamWriter output_cfg_file = new StreamWriter(test_cfg_file_path_name))
      {
        foreach (var lb_test_file_delected in lbTestFiles.CheckedItems)
          output_cfg_file.WriteLine(lb_test_file_delected.ToString());
      }
    }
    private string[] test_cfg_load()
    { 
      string[] test_cfg_files = Array.Empty<string>();
      string test_cfg_file_path_name = Path.Combine(_TestFilesPath, "test.cfg");
      if (File.Exists(test_cfg_file_path_name))
        test_cfg_files = File.ReadAllLines(test_cfg_file_path_name);
      return test_cfg_files;
    }


    bool Do_(AppSite app_site_, string grm_file_path_name_, string egt_file_path_name_, bool async_compute_)
    {      
      Builder? builder = Do_BuilserSetup(app_site_);
      if (builder == null)
        return false;

      string s_grammar = Do_GrammarRead(grm_file_path_name_); // просто считывание .grm файла в строку
      //

      (bool b_grammar_parse_ok, GrammarTables grammar_tables) = Do_ConstructGrammarTables(app_site_, builder, s_grammar);
      Debug.Assert(b_grammar_parse_ok);
      if (!b_grammar_parse_ok)
        return false;

      (bool b_grammar_analize_ok, BuilderTables builder_tables, BuilderSymbol? start_symbol) = Do_ConstructBuilderTables(app_site_, builder, grammar_tables);
      Debug.Assert(b_grammar_analize_ok && start_symbol != null);
      if (!b_grammar_analize_ok || start_symbol == null)
        return false;

      (bool b_lr_ok, BuilderLRStatesList lr_states, bool b_fa_ok, BuilderFACharsetsList fa_charsets, BuilderFAStatesList fa_states) compute_result;
      if (async_compute_)
        compute_result = Do_ComputeGrammarUseTask(app_site_, builder_tables, start_symbol);
      else
        compute_result = Do_ComputeGrammarUseSync(app_site_, builder_tables, start_symbol);
      Debug.Assert(compute_result.b_lr_ok && compute_result.b_fa_ok);
      if (!compute_result.b_lr_ok || !compute_result.b_fa_ok)
        return false;

      Do_ComputeComplete(builder_tables);
      
      bool b_store_ok = Do_SaveVer5(app_site_, egt_file_path_name_, builder_tables, compute_result.lr_states, compute_result.fa_charsets, compute_result.fa_states);
      if (!b_store_ok)
        return false;

      return true;
    }

    private void bnClose_Click(object sender, EventArgs e)
    {
      test_cfg_save();
      this.Close();
    }


    // for mofify "original" generate date
    //2025-04-04 19:17


    private void FrmMain_Load(object sender, EventArgs e)
    {
      String[] test_files = Directory.GetFiles(_TestFilesPath, "*.grm");
      foreach (string test_file in test_files)
        lbTestFiles.Items.Add(Path.GetFileNameWithoutExtension(test_file));

      if (lbTestFiles.Items.Count > 0)
      {
        string[] files_for_test = test_cfg_load();
        foreach (string test_file_name in files_for_test)
        {
          int item_index = lbTestFiles.FindString(test_file_name);
          if(item_index != ListBox.NoMatches)
            lbTestFiles.SetItemCheckState(item_index, CheckState.Checked);
        }
        if(lbTestFiles.CheckedItems.Count == 0)
          lbTestFiles.SetItemCheckState(0, CheckState.Indeterminate);
      }
      
      if (lbTestFiles.CheckedItems.Count == 0)
      {
        MessageBox.Show($"Failed to use test configuration in folder '{_TestFilesPath}'. Exiting..");
        return;
      }

      this.Show();

      Stopwatch tm_test = Stopwatch.StartNew();

      foreach (string s_grm_file_name in lbTestFiles.CheckedItems)
      {
        string s_grm_file_path_name = make_file_path_name_grm(s_grm_file_name);
        string s_egt_file_path_name = make_file_path_name_egt(s_grm_file_name);
        this.Text = s_grm_file_path_name;
        this.Refresh();
        
        TestAppSite my_app_site = new TestAppSite(new AppNotifyDummy(), new AppLog());
        m_test_log = "";

        m_tm_all.Start();
          bool b_do_ok = Do_(my_app_site, s_grm_file_path_name, s_egt_file_path_name, async_compute_: false);
        m_tm_all.Stop();
        TimeSpan tm_all = m_tm_all.Elapsed;

        LogView.ShowLog(my_app_site.Log);
        LogView.AddLogEx("TEST", "", $"Done '{s_grm_file_name}', time elapsed: " + tm_all.ToString(), m_test_log);
        this.Refresh();

        bool b_test_ok = b_do_ok;
        string s_message = "Ok";

        if (b_do_ok)
        {
          try
          {
            bool b_compare_ok = IsFilesEqual(s_egt_file_path_name, make_file_path_name_egt_original(s_grm_file_name));
            if (!b_compare_ok)
              s_message = $"Resulting {s_egt_file_path_name} not binary compatable with original !!!";
            b_test_ok = b_compare_ok;
          }
          catch (Exception ex_)
          {
            s_message = $"Exeption when try compare {s_egt_file_path_name} with original: " + ex_.Message;
            b_test_ok = false;
          }
        }
        else
          s_message = $"Process {s_grm_file_path_name} failed! See Log.";

        if (!b_test_ok)
        {
          MessageBox.Show(s_message);
          break;
        }
      }

      tm_test.Stop();
      TimeSpan tm = tm_test.Elapsed;
      if(lbTestFiles.CheckedItems.Count > 1)
        LogView.AddLogEx("TEST ALL", "", $"Done ALL, time elapsed: " + tm.ToString(), null);

      LogView.SelectLast();
    }




    void log_dump_clear()
    { }
    void log_dump_AppendTextLine(string text_) 
    {
      LogView.AddLogEx("DUMP", "", text_, null);
    }

    private void button2_Click(object sender, EventArgs e)
    {
      AppLog log = new AppLog();
      BuilderTablesView? t_org = BuilderDbLoader.LoadBuilderTablesView(log, @"D:\dev\gp_original\gp.egt");
      BuilderTablesView? t_new = BuilderDbLoader.LoadBuilderTablesView(log, @"D:\dev\gpp\test\test_files\GOLD Meta-Language (5.0.1).egt");

      log_dump_clear();
      if (log.LoggedCriticalError())
      {
        log_dump_AppendTextLine(log.DumpSection(AppLogSection.System));
        return;
      }

      bool b_ok = true;

      if (t_org.DFA.Count != t_new.DFA.Count)
        log_dump_AppendTextLine($"org DFA={t_org.DFA.Count}, new DFA={t_org.DFA.Count}");

      for (int i = 0; i < t_org.DFA.Count; i++)
      {
        cmp_DFA_accept(i, t_org.DFA[i], t_new.DFA[i]);
      }

      if (t_org.Group.Count != t_new.Group.Count)
        log_dump_AppendTextLine($"org Group.Count={t_org.Group.Count}, new Group.Count={t_new.Group.Count}");
      if (t_org.Group.Count == t_new.Group.Count)
      {
        for (int i = 0; i < t_org.Group.Count; i++)
        {
          cmp_group(i, t_org.Group, t_org.Group[i], t_new.Group[i]);
        }
      }


      if (b_ok)
        log_dump_AppendTextLine($"DFA OK");
    }
    void cmp_group(int group_index, BuilderGroupsList a_groups_, BuilderGroup a, BuilderGroup b)
    {
      if (a.TableIndex != b.TableIndex)
        log_dump_AppendTextLine($"{group_index} => A.TableIndex {a.TableIndex}, builder.TableIndex {b.TableIndex}");
      if (a.Name != b.Name)
        log_dump_AppendTextLine($"{group_index} => A.Name {a.Name}, builder.Name {b.Name}");
      if (a.Advance != b.Advance)
        log_dump_AppendTextLine($"{group_index} => A.Advance {a.Advance}, builder.Advance {b.Advance}");
      if (a.Ending != b.Ending)
        log_dump_AppendTextLine($"{group_index} => A.Ending {a.Ending}, builder.Ending {b.Ending}");

      if (a.Container.Name != b.Container.Name)
        log_dump_AppendTextLine($"{group_index} => A.Container {a.Container.Name}, builder.Container {b.Container.Name}");
      if (a.Start.Name != b.Start.Name)
        log_dump_AppendTextLine($"{group_index} => A.Start {a.Start.Name}, builder.Start {b.Start.Name}");
      if (a.End.Name != b.End.Name)
        log_dump_AppendTextLine($"{group_index} => A.End {a.End.Name}, builder.End {b.End.Name}");

      if (a.Nesting.Count != b.Nesting.Count)
      {
        log_dump_AppendTextLine($"group name: {a.Name}");
        log_dump_AppendTextLine($"{group_index} => A.Nesting.Count {a.Nesting.Count}, builder.Nesting.Count {b.Nesting.Count}");
        for (int i = 0; i < a.Nesting.Count; i++)
        {
          log_dump_AppendTextLine($"a nesting: {a_groups_[a.Nesting[i]].Name}");
        }
      }


    }
    void cmp_DFA_accept(int fa_index_, BuilderFAState a, BuilderFAState b)
    {
      if (a.Accept == null && b.Accept == null)
        return;

      if (a.Accept != null && b.Accept == null)
        log_dump_AppendTextLine($"{fa_index_} => DFA A accept {a.Accept.Name}, DFA builder accept NULL");
      else if (a.Accept == null && b.Accept != null)
        log_dump_AppendTextLine($"{fa_index_} => DFA A accept NULL, DFA builder accept {b.Accept.Name}");
      else if (a.Accept.TableIndex != b.Accept.TableIndex)
      {
        log_dump_AppendTextLine($"{fa_index_} => DFA A accept {a.Accept.Name}, DFA builder accept {b.Accept.Name}");
      }
      else
      {
        cmd_DFA_edges(fa_index_, a.Edges(), b.Edges());
      }
    }
    void cmd_DFA_edges(int fa_index_, BuilderFAEdgesList a_edges_, BuilderFAEdgesList b_edges_)
    {
      if (a_edges_.Count() != b_edges_.Count())
      {
        log_dump_AppendTextLine($"{fa_index_} => DFA A edges_.Count {a_edges_.Count()}, DFA builder edges_.Count {b_edges_.Count()}");
      }
      else
      {
        cmp_DFA_edges_list(fa_index_, a_edges_, b_edges_);
      }
    }
    void cmp_DFA_edges_list(int fa_index_, BuilderFAEdgesList a_edges_, BuilderFAEdgesList b_edges_)
    {
      for (int i = 0; i < a_edges_.Count(); i++)
      {
        if (a_edges_[i].TargetFAStateIndex != b_edges_[i].TargetFAStateIndex)
        {
          log_dump_AppendTextLine($"{fa_index_} => DFA A edges_.TargetFAStateIndex {a_edges_[i].TargetFAStateIndex}, DFA builder edges_.TargetFAStateIndex {b_edges_[i].TargetFAStateIndex}");
        }
        if (!a_edges_[i].Characters.IsEqualSet(b_edges_[i].Characters))
        {
          log_dump_AppendTextLine($"{fa_index_} => charsets not equal ! ");
        }
      }
    }

    internal static void dump_build_symbols(BuilderTables builder_tables_)
    {
      Debug.WriteLine("== dump_build_symbols ==");
      int c_symbols = builder_tables_.Symbol.Count();
      Debug.WriteLine("count = " + c_symbols);
      for (int i_symbol = 0; i_symbol < c_symbols; ++i_symbol)
      {
        BuilderSymbol s = builder_tables_.Symbol[i_symbol];
        Debug.WriteLine(s.Name /*+ " : " + s.setitem_type_*/);
      }
    }

    public static void check_sorting()
    {
      string s1 = "#";
      string s2 = ",";
      //bool b_String_Compare = String.Compare(s1, s2) < 0;
      bool b_String_Compare_StringSort = String.Compare(s1, s2, null, CompareOptions.StringSort) < 0;
      Debug.WriteLine("b_String_Compare_StringSort: " + b_String_Compare_StringSort);
      bool b_String_CompareOrdinal = String.CompareOrdinal(s1, s2) < 0;
      Debug.WriteLine("b_String_CompareOrdinal: " + b_String_CompareOrdinal);

      bool b_CultureInfo_CurrentCulture_CompareInfo_Compare_OptNone = CultureInfo.CurrentCulture.CompareInfo.Compare(s1, s2, CompareOptions.None) < 0;
      Debug.WriteLine("b_CultureInfo_CurrentCulture_CompareInfo_Compare_OptNone: " + b_CultureInfo_CurrentCulture_CompareInfo_Compare_OptNone);

      CultureInfo culture = CultureInfo.CurrentCulture;
      Debug.WriteLine("culture: " + culture.Name);
      CompareInfo compare = culture.CompareInfo;
      Debug.WriteLine("compare: " + compare.Name + ", LCID: " + compare.LCID + ", sort_ver: " + compare.Version.FullVersion + ", sort_id: " + compare.Version.SortId);

      Debug.WriteLine("ICUMode: " + IsGlobalizationICUMode());
      //System.Globalization.
      //GlobalizationMode.UseNls
    }
  } // FrmMain


  internal class TestAppSite(AppNotify app_notify_, AppLog app_log_) : AppSite
  {
    private readonly AppNotify  m_Notify  = app_notify_;
    private readonly AppLog     m_Log     = app_log_;
    //
    public AppNotify  Notify    => m_Notify;
    public AppLog     Log       => m_Log;
    public AppSite CreateAppSiteForThread()
    {
      return new TestAppSite(new AppNotifyDummy(), new AppLog());
    }
    public void CombineLog(params AppLog[] other_logs_)
    { 
      m_Log.Append(other_logs_);
    }
    //
  }

}   // namespace gpp_maker

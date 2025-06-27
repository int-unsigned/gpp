//
using AFX;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static gpp.builder.GrammarTables;
using static System.Runtime.InteropServices.JavaScript.JSType;

//
//
namespace gpp.builder;


public enum CharMappingMode
{
  Invalid     = -1, // 0xFFFFFFFF
  Windows1252 = 0,
  None        = 1,
}

public enum CharSetMode
{
  Invalid = -1, // 0xFFFFFFFF
  Unicode = 0,
  ANSI    = 1,
}


public static class BuilderInfo
{ 
  // Оригинальное
  public static string    APP_NAME                    => "GOLD Parser Builder";
  public static string    APP_VERSION_GP_DAT_5_0_1    => "5.0.1";
  public static string    APP_VERSION_FULL            => "5.2.0";
  public static string    APP_VERSION_TITLE           => "5.2";
  public static string    APP_DESCRIPTION             => "Grammar Oriented Language Developer";
  public static string    APP_NAME_VERSION_FULL       => $"{APP_NAME} {APP_VERSION_FULL}.";   //NOTE  Tail '.' mandatory for .egt binary compatibility
  public static string About() 
  {
    return "" + "Admittedly, this is not a particularly clever acronym, but it does (in part) represent the history of the greater Sacramento Area." 
              + "\r\n\r\n" + "This application is shareware. You are completely FREE to use this application for your projects, but I hope that you will SHARE your feedback and suggestions with me!" 
              + "\r\n\r\n" + "Only with your input will this become a respected and useful programmer's tool. Happy programming!";
  }
}



internal class Builder
{
  private GrammarParser           m_GrammarParser;
  private PreDefinedCharsetsList  m_PredefinedSets;
  private UnicodeTable            m_PredefinedUnicodeTable;
  //
  //
  //
  private Builder(GrammarParser grammar_parser_, PreDefinedCharsetsList predefined_charsets_, UnicodeTable predefined_unicode_table_)
  { 
    m_GrammarParser           = grammar_parser_;
    m_PredefinedSets          = predefined_charsets_;
    m_PredefinedUnicodeTable  = predefined_unicode_table_;
  }
  //
  public PreDefinedCharsetsList   PredefinedSets          => m_PredefinedSets;
  public UnicodeTable             PredefinedUnicodeTable  => m_PredefinedUnicodeTable;
  public GrammarParser            GrammarParser           => m_GrammarParser;
  //
  public static Builder? Create(AppLog log_, string gpp_egt_tables_file_path_name_, string gpp_predefined_sets_file_path_name_, string gpp_unicode_mapping_file_path_name_)
  {
    try
    {
      GrammarParser? grammar_parser = GrammarParser.Create(log_, gpp_egt_tables_file_path_name_);
      if (grammar_parser == null)
        return null;
      PreDefinedCharsetsList predefined_charsets = Builder.LoadPredefinedSets(log_, gpp_predefined_sets_file_path_name_);
      UnicodeTable predefined_unicode_table = Builder.LoadUnicodeTable(log_, gpp_unicode_mapping_file_path_name_);

      return new Builder(grammar_parser, predefined_charsets, predefined_unicode_table);
    }
    catch (Exception e)
    {
      log_.Add(AppLogSection.Internal, AppLogAlert.Critical, $"Builder can`t be created: '{e.Message}'");
      return null;
    }
  }
  //

  private class PreDefinedCharsetsListCtor(THashSetSimpleUnsafe<PreDefinedCharset> data_, PreDefinedCharset charset_Whitespace_, PreDefinedCharset charset_AllWhitespace_)
    : PreDefinedCharsetsList(data_, charset_Whitespace_, charset_AllWhitespace_)
  { }
  private class PreDefinedCharsetsListLoader(size_t count_)
  {
    private class EqComparer : AFX.IEqualityComparerAB<PreDefinedCharset, PreDefinedCharset>
    {
      public bool IsEqual(ref readonly PreDefinedCharset a_item_, ref readonly PreDefinedCharset b_item_)
        => a_item_.Identificator.IsEqualCode(in b_item_.Identificator);
      public static readonly EqComparer Instance = new();
    }
    //
    private THashSetSimpleUnsafe<PreDefinedCharset> m_data = new THashSetSimpleUnsafe<PreDefinedCharset>(count_, EqComparer.Instance);
    //
    public bool AddFromLoader(PreDefinedCharset item_) 
    {
      ref readonly PreDefinedCharset existing_item = ref m_data.Add(item_.Identificator.Hash, ref item_);
      return Unsafe.IsNullRef<PreDefinedCharset>(in existing_item);
    } 
    //
    public PreDefinedCharsetsList CreatePreDefinedCharsetsList(PreDefinedCharset charset_whitespace_, PreDefinedCharset charset_all_whitespace_)
    {
      return new PreDefinedCharsetsListCtor(m_data, charset_whitespace_, charset_all_whitespace_);
    }
    public static PreDefinedCharset CreateDummyCharset()
    { 
      return PreDefinedCharset.CreateDummyCharset();
    }
    public PreDefinedCharsetsList CreatePreDefinedCharsetsListDummy()
    {
      return CreatePreDefinedCharsetsList(CreateDummyCharset(), CreateDummyCharset());
    }
  }

  private class PreDefinedCharsetLoader : PreDefinedCharset
  {
    private PreDefinedCharsetLoader(ref readonly TIdentificator identificator_, string name_, string type_, string comment_)
      :base (in identificator_, name_, type_, comment_/*, null*/) 
    { }
    public static PreDefinedCharsetLoader Create(string name_, string type_, string comment_)
    { 
      TIdentificator identificator = MakeIdentificator(name_);
      return new PreDefinedCharsetLoader(ref identificator, name_, type_, comment_);
    }
    public bool AddRangeFromEgt(AppLog log_, int ch_begin_, int ch_final_)
    {
      bool b_valid = is_valid_range(ch_begin_, ch_final_);
      if (b_valid)
        base.AddRangeFromLoader(ch_begin_, ch_final_);
      else
        log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'sets.dat' contains invalid range.");

      return b_valid;        
    }
    public bool AddRangeFromEgtNext(AppLog log_, ref int in_out_char_final_prev_, int ch_begin_, int ch_final_)
    {
      if (ch_begin_ > in_out_char_final_prev_)
      {
        if (AddRangeFromEgt(log_, ch_begin_, ch_final_))
        {
          in_out_char_final_prev_ = ch_final_;
          return true;
        }
      }
      else 
        log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'sets.dat' contains invalid ranges sequence.");
        
      return false;
    }
  }

  private static PreDefinedCharsetsList LoadPredefinedSets(AppLog log_, string gpp_predefined_sets_file_path_name_)
  {
    const string s_GOLD_Character_Sets = "GOLD Character Sets";

    PreDefinedCharsetsListLoader predefined_charsets_list = new PreDefinedCharsetsListLoader(175);

    SimpleDB.Reader reader = new SimpleDB.Reader();

    reader.Open(gpp_predefined_sets_file_path_name_);
    if (reader.Header() != s_GOLD_Character_Sets)
    {
      log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'sets.dat' is invalid");
      return predefined_charsets_list.CreatePreDefinedCharsetsListDummy();
    }
    else
    {
      PreDefinedCharset?  charset_whitespace_or_null      = null;
      PreDefinedCharset?  charset_all_whitespace_or_null  = null;
      int                   charset_index                   = 0;
      while (!reader.EndOfFile())
      {
        reader.GetNextRecord();
        string  s_name     = reader.RetrieveString();
        string  s_type     = reader.RetrieveString();
        string  s_comment  = reader.RetrieveString();
        int     c_ranges   = reader.RetrieveInt16();
        PreDefinedCharsetLoader defined_charset = PreDefinedCharsetLoader.Create(s_name, s_type, s_comment);

        if (c_ranges > 0)
        {
          char_t ch_begin = reader.RetrieveInt16();
          char_t ch_final = reader.RetrieveInt16();
          if (defined_charset.AddRangeFromEgt(log_, ch_begin, ch_final))            
          {
            for (size_t i_range = 1; i_range < c_ranges; ++i_range)
            {
              if (!defined_charset.AddRangeFromEgtNext(log_, ref ch_final, reader.RetrieveInt16(), reader.RetrieveInt16()))
                break;
            }
          }
        }
        if (!reader.RecordComplete())
          log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'sets.dat' structure corrupt.");
                 
        //TODO  мы здесь жестко привязываемся к структуре 'sets.dat' и об этом нужно !незабыть
        if (charset_index == 8)
        {
          Debug.Assert(s_name == "Whitespace");
          if (s_name == "Whitespace")        
            charset_whitespace_or_null = defined_charset;
          else
            log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'sets.dat' must have 'Whitespace' charset at record 8.");
        }
        else if (charset_index == 172)
        {
          Debug.Assert(s_name == "All Whitespace");
          if (s_name == "All Whitespace")
            charset_all_whitespace_or_null = defined_charset;
          else
            log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'sets.dat' must have 'All Whitespace' charset at record 172.");
        }

        if(!predefined_charsets_list.AddFromLoader(defined_charset))
          log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "Duplicated predefined charset");
        ++charset_index;
      }
      reader.Close();
      
      PreDefinedCharset charset_whitespace;
      if(charset_whitespace_or_null == null)
      {
        log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'sets.dat' must have 'Whitespace' charset.");
        charset_whitespace = PreDefinedCharsetsListLoader.CreateDummyCharset();
      }
      else
        charset_whitespace = charset_whitespace_or_null;

      PreDefinedCharset charset_all_whitespace;
      if (charset_all_whitespace_or_null == null)
      {
        log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'sets.dat' must have 'All Whitespace' charset.");
        charset_all_whitespace = PreDefinedCharsetsListLoader.CreateDummyCharset();
      }
      else
        charset_all_whitespace = charset_all_whitespace_or_null;

      return predefined_charsets_list.CreatePreDefinedCharsetsList(charset_whitespace, charset_all_whitespace);
    }

    
  }
  //
  private static UnicodeTable LoadUnicodeTable(AppLog log_, string gpp_unicode_mapping_file_path_name_)
  {
    const string s_PREFIX_C = "C";
    const string s_PREFIX_W = "W";
    const string s_GOLD_Character_Mapping = "GOLD Character Mapping";
    SimpleDB.Reader reader = new SimpleDB.Reader();
    reader.Open(gpp_unicode_mapping_file_path_name_);

    //TODO  В .dat не записано кол-во элементов. Поскольку файл .dat фиксирован я из просто посчитал (см. в кнце метода)
    UnicodeMapTable unicode_lower_table   = new UnicodeMapTable(675);
    UnicodeMapTable unicode_upper_table   = new UnicodeMapTable(675);
    UnicodeMapTable win1252_table         = new UnicodeMapTable(54);

    void _add_case(int UppercaseCode, int LowercaseCode/*, string Name*/)
    {
      unicode_lower_table.Add(LowercaseCode, UppercaseCode);
      unicode_upper_table.Add(UppercaseCode, LowercaseCode);
    }
    void _add_win1252(int CharCode, int Mapping)
    {
      //TODO  Как-то непонял логику. Что значит этот "двунаправленный" мап ?
      win1252_table.Add(CharCode, Mapping);
      win1252_table.Add(Mapping, CharCode);
    }

    if (reader.Header() != s_GOLD_Character_Mapping)
      log_.Add(AppLogSection.Internal, AppLogAlert.Critical, "The file 'mapping.dat' is invalid.");
    else
    {
      while (!reader.EndOfFile())
      {
        reader.GetNextRecord();
        string s_prefix = reader.RetrieveString();  //TODO Для ридера лучше здесь, скажем, RetrieveChar
        int char_code_1 = reader.RetrieveInt16();
        int char_code_2 = reader.RetrieveInt16();
        if (s_prefix == s_PREFIX_C)
          _add_case(char_code_1, char_code_2/*, ""*/);
        else if (s_prefix == s_PREFIX_W)
          _add_win1252(char_code_1, char_code_2);
        else
        {
          //TODO  Здесь ошибка формата должна быть
          Debug.Assert(false, $"!> Unexpected mapping record prefix: {s_prefix}");
        }
      }
      reader.Close();
    }

    //Debug.WriteLine($"unicode_lower_table Count={unicode_lower_table.Count()}");
    //Debug.WriteLine($"unicode_upper_table Count={unicode_upper_table.Count()}");
    //Debug.WriteLine($"win1252_table Count={win1252_table.Count()}");
    /*
      unicode_lower_table Count=675
      unicode_upper_table Count=675
      win1252_table Count=54
    */

    return new UnicodeTable(unicode_lower_table, unicode_upper_table, win1252_table);
  }


  public (bool, GrammarTables) ParseGrammar(AppSite app_, TextReader user_grammar_source_)
  {
    Debug.Assert(!app_.Log.LoggedCriticalError());

    app_.Notify.Mode = AppProgramMode.Input;
    app_.Notify.Started("Parsing Grammar");
    //
    (bool b_parsing_accepted, GrammarTables grammar_tables) = m_GrammarParser.DoParse(app_, this.PredefinedSets, user_grammar_source_);
    //
    app_.Notify.Completed("Parsing Grammar Completed");
    app_.Notify.Mode = AppProgramMode.Idle;

    return (b_parsing_accepted && !app_.Log.LoggedCriticalError(), grammar_tables);
  }

}


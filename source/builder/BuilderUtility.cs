//
using System.Diagnostics;
using System.Text;


//
//
namespace gpp.builder;



internal sealed class BuilderUtility
{

  internal static bool IsValidPropertyName(string Name)
  {
    string upper = Name.ToUpper();
    //TODO  Здесь прямо с предефайнед хэшсетом работать надо!
    return upper.Equals("NAME") || upper.Equals("VERSION") || upper.Equals("ABOUT") || upper.Equals("AUTHOR") || upper.Equals("START SYMBOL")
      || upper.Equals("AUTO WHITESPACE") || upper.Equals("CHARACTER MAPPING") || upper.Equals("CASE SENSITIVE") || upper.Equals("VIRTUAL TERMINAL");
  }


  public static string TimeDiffString(DateTime StartTime, DateTime EndTime)
  {
    TimeSpan interval = EndTime - StartTime;
    return interval.ToString();
    //TODO  Оригинально использовалось "красивое" форматирование. При желании можно сделать..
    //return Conversions.ToString(Conversion.Int((double) DateAndTime.DateDiff(DateInterval.Minute, StartTime, EndTime) / 60.0))  + " Hours " 
    //  + Conversions.ToString(Conversion.Int(DateAndTime.DateDiff(DateInterval.Minute, StartTime, EndTime) % 60L)) + " Minutes " 
    //  + Conversions.ToString(Conversion.Int(DateAndTime.DateDiff(DateInterval.Second, StartTime, EndTime) % 60L)) + " Seconds";
  }


  public static string DisplayText(BuilderFACharset charset_,    bool ReplaceSpaceChar = true,    int MaxSize = 1024,    string OversizeMessage = "",    short BreakWidth = -1)
  {
    //TODO  а) Не понятна разница в текстовом представлении
    //      б) почему чтобы это понять нужно целый charset_.RangeList() делать - это НАКЛАДНО !
    CharsRangeList charset_as_range_list = charset_.RangeList();
    if (IsDisplayableRangeList(charset_as_range_list))
      return charset_.GetDisplayString();
    else
      return BuilderUtility.DisplayRangeListText(charset_as_range_list);
  }
  //TODO  Почему дискриминация??
  private static bool IsDisplayableRangeList(CharsRangeList range_list_)
  {
    for (int i = 0; i < range_list_.Count; ++i)
      if (!IsDisplayableRange(ref range_list_[i]))
        return false;
    return true;
  }
  private static bool IsDisplayableRange(ref readonly CharsRange range_)
  {
    return IsDisplayableRange(range_.char_begin, range_.char_final);
  }

  private static bool IsDisplayableRange(int First, int Last)
  {
    return First >= 32 & First <= (int) sbyte.MaxValue & Last >= 32 & Last <= (int) sbyte.MaxValue | First >= 160 & First <= (int) byte.MaxValue & Last >= 160 & Last <= (int) byte.MaxValue;
  }

  public static string DisplayRangeListText(CharsRangeList ranges_)
  {
    if (ranges_.Count > 0)
    {
      string s_result = BuilderUtility.DisplayRangeText(ranges_[0]);
      for (int i = 1; i < ranges_.Count; ++i)
        s_result += (", " + BuilderUtility.DisplayRangeText(ranges_[i]));
      return  s_result;
    }
    else
      return string.Empty;
  }

  public static string DisplayRangeText(CharsRange range_)
  {
    //TODO упростить
    return (range_.char_begin != range_.char_final) ?
      (checked(range_.char_final - range_.char_begin) != 1 ? "&" + BuilderUtility.DisplayCodeText(range_.char_begin) + " .. &" + BuilderUtility.DisplayCodeText(range_.char_final)
      : "&" + BuilderUtility.DisplayCodeText(range_.char_begin) + ", &" + BuilderUtility.DisplayCodeText(range_.char_final)) : "&" + BuilderUtility.DisplayCodeText(range_.char_begin);
  }

  private static string DisplayCodeText(int Codepoint)
  {
    string str = Codepoint.ToHexString();
    if (str.Length % 2 == 1)
      str = "0" + str;
    return str;
  }

  public static string DisplayText(string Text, bool ReplaceSpaceChar = true, int MaxSize = 1024, string OversizeMessage = "")
  {
    string s_result;
    if (Text.Length > MaxSize)
    {
      s_result = !OversizeMessage.Empty()? OversizeMessage : "The text is too large to view: " + Text.Length + " characters";
    }
    else
    {
      s_result = "";
      for (int i = 0; i < Text.Length; ++i)
      {
        string s_char = BuilderUtility.DisplayChar((int)Text[i], ReplaceSpaceChar);
        s_result += s_char;
      }
    }
    return s_result;
  }

  public static string DisplayChar(int char_code_, bool b_replace_space_char_)
  {
    //TODO  Вроде у шарпа есть понятие имени символа. надо смотреть и переделать..
    switch (char_code_)
    {
      case 9:        return "{HT}";
      case 10:       return "{LF}";
      case 11:       return "{VT}";
      case 12:       return "{FF}";
      case 13:       return "{CR}";
      case 32:       return (b_replace_space_char_) ? "{Space}" : " ";
      case 160:      return "{NBSP}";
      case 8364:     return "{Euro Sign}";
      default:       return !(char_code_ >= 32 & char_code_ <= 126 || char_code_ >= 160 && char_code_ <= (int) byte.MaxValue) ? "{#" + char_code_ + "}" : vb_compatable_ChrW(char_code_).ToString();
    }
  }


  internal enum RangeCompareResult
  {
    Subset,
    Superset,
    LessThanDisjoint,
    LessThanOverlap,
    GreaterThanDisjoint,
    GreaterThanOverlap,
  }
  //
  internal static RangeCompareResult RangeCompare(int a_begin_, int a_final_, int b_begin_, int b_final_)
  {
    RangeCompareResult range_compare_result = 0;

    if (a_begin_ < b_begin_ && a_final_ > b_final_)
      range_compare_result = RangeCompareResult.Superset;
    else if (b_begin_ < a_begin_ && b_final_ > a_final_)
      range_compare_result = RangeCompareResult.Subset;
    else if (a_final_ < b_begin_)
      range_compare_result = RangeCompareResult.LessThanDisjoint;
    else if (a_begin_ < b_begin_ && a_final_ < b_final_)
      range_compare_result = RangeCompareResult.LessThanOverlap;
    else if (b_final_ < a_begin_)
      range_compare_result = RangeCompareResult.GreaterThanDisjoint;
    else if (b_begin_ < a_begin_ && b_final_ < a_final_)
      range_compare_result = RangeCompareResult.GreaterThanOverlap;

    return range_compare_result;
  }


  internal enum RangeRelationResult
  {
    Subset,
    Superset,
    Disjoint,
    Overlap,
  }
  //
  internal static RangeRelationResult RangeRelation(int a_begin_, int a_final_, int b_begin_, int b_final_)
  {
    //TODO разобрать понятнее
    return !(a_final_ < b_begin_ || a_begin_ > b_final_) ?
          (!(a_begin_ >= b_begin_ && a_final_ <= b_final_) ?
              (!(a_begin_ < b_begin_ && a_final_ > b_final_) ?
                RangeRelationResult.Overlap : RangeRelationResult.Superset) 
                  : RangeRelationResult.Subset) 
                    : RangeRelationResult.Disjoint;
  }





  //TODO  Не Вызывается. Видимо какой-то артефакт, т.к. для "_regexp_variable_lenght" имеются другие рабочие методы
  public static bool RuleTypeExists(BuilderProductionsList productions_, BuilderSymbol symbol_non_terminal_)
  {
    bool b_symbol_has_regexp_variable_lenght = false;
    short production_index = 0;
    while (!b_symbol_has_regexp_variable_lenght & (int)production_index < productions_.Count())
    {
      if (productions_[production_index].Head.IsTheSame(symbol_non_terminal_))
        b_symbol_has_regexp_variable_lenght = true;
      checked { ++production_index; }
    }
    return b_symbol_has_regexp_variable_lenght;
  }



  ///////////////////////////
  // CGT stuff (из BuilderApp)
  // По идее должно проверять перед сохранением в старом формате .cgt
  public static void PopulateSaveCGTWarningTable(AppLog log_, BuilderGroupsList groups_, BuilderFACharsetsList chasets_)
  {
    const string s_COMMENT_BLOCK  = "COMMENT BLOCK";
    const string s_COMMENT_LINE   = "COMMENT LINE";
       
    int num1 = checked (groups_.Count - 1);
    int Index1 = 0;
    while (Index1 <= num1)
    {
      BuilderGroup groupBuild = groups_[Index1];
      string group_NAME = groupBuild.Name.ToUpper();
      if( group_NAME != s_COMMENT_BLOCK && group_NAME != s_COMMENT_LINE)
        log_.Add("The self_grammar_group '" + groupBuild.Name + "' will not be saved", "Version 1.0 only supports one self_grammar_group: Comment. The start/end symbols will be saved as regular terminals.");
      checked { ++Index1; }
    }
    int Index2 = groups_.ItemIndex("COMMENT BLOCK");
    if (Index2 != -1)
    {
      BuilderGroup groupBuild = groups_[Index2];
      if (groupBuild.Nesting.Count != 2)
        log_.Add("Comment Block attribute will change", "Version 1.0 only supports 'all' nested block comments. When the file is saved, it will use this attribute.");
      if (groupBuild.Advance != GroupAdvanceMode.Token)
        log_.Add("Comment Block attribute will change", "Version 1.0 only supports 'token' advancing in block comments. When the file is saved, it will use this attribute.");
      if (groupBuild.Ending != GroupEndingMode.Closed)
        log_.Add("Comment Block attribute will change", "Version 1.0 only supports 'closed' block comments. When the file is saved, it will use this attribute.");
    }

    if (chasets_.Count() > 0)
    {
      long num2 = 0;
      long num3 = 0;
      for(int charset_index = 0; charset_index < chasets_.Count(); ++charset_index)
      { //TODO  нужно красивее "bytes to store character set data" считать
        checked { num2 += (long)(5 + chasets_[charset_index].CalculateCharsCount() * 2 + 2); }
        checked { num3 += (long)(12 + 6 * chasets_[charset_index].RangeList().Count); }
      }
      if ((double) num2 / (double) num3 >= 2.0)
        log_.Add(AppLogSection.Grammar, AppLogAlert.Info, "Version 1.0 will require " + num2.ToString() 
          + " bytes to store character set data. The new format will require " + num3.ToString() + " bytes.");
    }

    bool flag = false;
    int Index4 = 0;
    while (Index4 < chasets_.Count() & !flag)
    {
      if (chasets_[Index4].Contains(0))
        flag = true;
      checked { ++Index4; }
    }
    if (!flag)
      return;
    log_.Add("Character &00 cannot be stored", "Due to how character sets are stored in Version 1.0, null characters (&00) will not stored.");
  }

  // Вынесено из ParseTablesBuild. Видимо какой-то артефакт прошлых версий.
  public static void ComputeCGTMetadata(BuilderSymbolsList symbols_, BuilderFAStatesList DFA_)
  {
    for (int symbol_index = 0; symbol_index < symbols_.Count(); ++symbol_index)
      symbols_[symbol_index].UsesDFA = false;

    for (int dfa_index = 0; dfa_index < DFA_.Count; ++dfa_index)
    {
      if (DFA_[dfa_index].Accept != null)
        symbols_[DFA_[dfa_index].Accept.TableIndex].UsesDFA = true;
    }
  }
  // End CGT stuff (из BuilderApp)
  ////////////////////////////////


  public static void SaveLog(AppLog log_, string FilePath)
  {
    TextWriter textWriter = (TextWriter)new StreamWriter(FilePath, false, Encoding.UTF8);

    for (int log_index = 0; log_index < log_.Count(); ++log_index)
    {
      AppLogItem sysLogItem = log_[log_index];
      string str1 = sysLogItem.SectionName().PadRight(15) + sysLogItem.AlertName().PadRight(10);
      string str2 = (sysLogItem.Index != null ? str1 + sysLogItem.Index.ToString().PadRight(8) : str1 + vb_compatable_Space(8)) + sysLogItem.Title + " : " + sysLogItem.Description;
      textWriter.WriteLine(str2);
    }
    textWriter.Close();
  }


  public static void LogActionTotals(AppLog log_, BuilderLRStatesList LALR_)
  {          
    int count_act_shift   = 0;
    int count_act_reduce  = 0;
    int count_act_goto    = 0;
    int count_act_accept  = 0;
    
    for(int fa_state_index = 0; fa_state_index < LALR_.Count; ++fa_state_index) 
    {
      BuilderLRState lr_state = LALR_[fa_state_index];
      for(int lr_state_act_index = 0; lr_state_act_index < lr_state.CountOfLRActions(); ++lr_state_act_index) 
      {
        BuilderLRAction lr_state_act = lr_state[lr_state_act_index];
        switch (lr_state_act.Type)
        {
          case LRActionType.Shift:
            ++count_act_shift;
            break;
          case LRActionType.Reduce:
            ++count_act_reduce;           
            break;
          case LRActionType.Goto:
            ++count_act_goto;
            break;
          case LRActionType.Accept:
            ++count_act_accept;
            break;
        }
      }
    }

    string s_log_title = "Total actions: " 
      + count_act_shift.ToString() + " Shifts, " 
      + count_act_reduce.ToString() + " Reduces, " 
      + count_act_goto.ToString() + " Gotos, " 
      + count_act_accept.ToString() + " Accepts.";

    log_.Add(AppLogSection.LALR, AppLogAlert.Detail, s_log_title);
  }




  public static BuilderFACharset? GetUserDefinedOrPredefinedCharacterSet(PreDefinedCharsetsList builder_predefined_character_sets_, UserDefinedCharsetsList user_defined_charsets_, string name_)
  {
    BuilderFACharset? charset = user_defined_charsets_.ItemByName(name_);
    if (charset == null)
      charset = builder_predefined_character_sets_.ItemByName(name_);
    //
    return charset;
  }


} // class BuilderUtility

//
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

//
//
namespace gpp.builder;


internal static class BuilderUiExtensions
{
  public static string Text(this LRConfigTrack this_)
  {
    string s_prefix_from_first  = (this_.FromFirst)? "F" : "-";
    string s_prefix_from_config = (this_.FromConfig)? "C" : "-";
    string s_prefix             = s_prefix_from_first + s_prefix_from_config + " : ";

    string parent_text          = this_.Parent.Text("^");

    return s_prefix + parent_text;
  }

  public static string Text(this LRConfig this_, string Marker /*= "^"*/)
  {
    string str = "<" + this_.ParentProduction.Head.Name + "> ::=";

    for(int handle_index = 0; handle_index < this_.ParentProduction.Handle().Count(); ++handle_index)
    {
      if ((int) handle_index == (int)this_.Position)
        str = str + " " + Marker;
      str = str + " " + this_.ParentProduction.Handle()[handle_index].Text();
    }
    if ((int)this_.Position > checked (this_.ParentProduction.Handle().Count() - 1))
      str = str + " " + Marker;
    return str;
  }


  //TODO  это нужно сделать для енумов, а не классов (BuilderGroup сюда не попадает)
  public static string AdvanceName(this gpp.parser.ParserGroup this_)
  {
    switch (this_.Advance)
    {
      case GroupAdvanceMode.Token:        return "Token";
      case GroupAdvanceMode.Character:    return "Character";
      default:                            return "Invalid";
    }
  }
  public static string EndingName(this gpp.parser.ParserGroup this_)
  {
    switch (this_.Ending)
    {
      case GroupEndingMode.Open:        return "Open";
      case GroupEndingMode.Closed:      return "Closed";
      default:                          return "Invalid";
    }
  }

  public static string RangeText(this BuilderFACharset this_, string chars_range_representation_, string Separator /*= ","*/, string Prefix = "", bool HexFormat = false)
  {
    //TODO  Используется в RegExpItem::ToString, которая вроде никем вообще не вызывается
    //      пс. реализацию надо в NumberSet смотреть
    return "{_RangeText_NOT_IMPLEMENTED_}";
  }

  public static string GetDisplayString(this BuilderFACharset this_)
  {
    size_t c_chars = this_.CalculateCharsCount();
    // assume BuilderUtility.DisplayChar return ~~ {#1234}
    StringBuilder sb = new StringBuilder(c_chars * 7);

    foreach (char_t ch in this_)
      sb.Append(BuilderUtility.DisplayChar(ch, true));

    return sb.ToString();
  }


  public static string ToString(this TerminalExpressionItem this_)
  { 
    if (this_.Data is TerminalExpression data_as_regexp)
      return "(" + data_as_regexp.ToString() + ")" + this_.KleeneOp.ToKleeneChar();
    else if (this_.Data is CharsetExpressionItem data_as_charset_expr_item)
    {
      switch (data_as_charset_expr_item.Type)
      {
        case CharsetExpressionItemType.Chars:     return "{" + data_as_charset_expr_item.Characters.RangeText("..", ", ", "&", true) + "}" + this_.KleeneOp.ToKleeneChar();
        case CharsetExpressionItemType.Name:      return "{" + data_as_charset_expr_item.Text + "}" + this_.KleeneOp.ToKleeneChar();
        case CharsetExpressionItemType.Sequence:  return LiteralFormat(data_as_charset_expr_item.Text, always_delimit_: false) + this_.KleeneOp.ToKleeneChar();
      }
    }
    //TODO we never be here
    Debug.Assert(false);
    return "";
  }




  //TODO  И CreateLRPriorStateLists и CreateDFAPriorStateLists не нужны собственно для построителя
  //      Это "хелперы" для окон редактора чтобы показывать "предыдущее состояние"
  public static void CreateLRPriorStateLists(BuilderLRStatesList in_out_LALR_)
  {
    for (int lr_index = 0; lr_index < in_out_LALR_.Count; ++lr_index)
      in_out_LALR_[lr_index].PriorStates.Clear();

    for (int lr_index = 0; lr_index < in_out_LALR_.Count; ++lr_index)
    {
      BuilderLRState lr_state = in_out_LALR_[lr_index];
      for (int lr_state_act_index = 0; lr_state_act_index < lr_state.CountOfLRActions(); ++lr_state_act_index)
      {
        BuilderLRAction lr_state_act = lr_state[lr_state_act_index];
        switch (lr_state_act.Type)
        {
          case LRActionType.Shift:
          case LRActionType.Goto:
            in_out_LALR_[(int)lr_state_act.Value()].PriorStates.Add(lr_index);
            break;
        }
      }
    }
  }
  //TODO  см.выше
  public static void CreateDFAPriorStateLists(BuilderFAStatesList DFA_)
  {
    for (int fa_index = 0; fa_index < DFA_.Count; ++fa_index)
      DFA_[fa_index].PriorStates.Clear();

    for (int fa_index = 0; fa_index < DFA_.Count; ++fa_index)
    {
      BuilderFAState fa_state = DFA_[fa_index];
      for (int fa_state_edge_index = 0; fa_state_edge_index < fa_state.Edges().Count(); ++fa_state_edge_index)
      {
        BuilderFAEdge edge = fa_state.Edges()[fa_state_edge_index];
        DFA_[edge.TargetFAStateIndex].PriorStates.Add(fa_index);
      }
    }
  }


  // Этот метод начала режима просмотра egt-файла. то есть это не построитель, а просмотр уже построенных таблиц
  public static BuilderTablesView? LoadEgtFile(AppLog log_, string egt_file_path_name_)
  {
    BuilderTablesView? builder_tables = BuilderDbLoader.LoadBuilderTablesView(log_, egt_file_path_name_);;
    if (builder_tables != null)
    {
      CreateDFAPriorStateLists(builder_tables.DFA);
      CreateLRPriorStateLists(builder_tables.LALR);
    }
    return builder_tables;
  }





  //TODO  Данные методы вынесены из BuildLR т.к. там они не использовались.
  //      Поскольку они "приват", то они не для UI. видимо какой-то артефакт или задел на будущее
  //      пусть пока здесь побудут. впоследствии или задействовать или удалить.
  private static void BuildLR_StateNumber(BuilderLRStatesList _LALR, BuilderLRState State, short Number, LRConfigCompare Status)
  {
    short num = -1;
    short Index = 0;
    LRConfigCompare lrConfigCompare = 0;
    while ((int)Index < _LALR.Count & num == (short)-1)
    {
      lrConfigCompare = State.ConfigSet.CompareCore(_LALR[(int)Index].ConfigSet);
      if (lrConfigCompare != LRConfigCompare.UnEqual)
        num = Index;
      checked { ++Index; }
    }
    Number = num;
    if (num == (short)-1)
      Status = LRConfigCompare.UnEqual;
    else
      Status = lrConfigCompare;
  }

  private struct BuildLR_StateTableInfoType
  {
    public int Index;
    public LRConfigCompare Compare;
  }
  private static BuildLR_StateTableInfoType BuildLR_GetStateInfo(BuilderLRStatesList _LALR, BuilderLRState State)
  {
    BuildLR_StateTableInfoType stateInfo;
    stateInfo.Index = -1;
    stateInfo.Compare = LRConfigCompare.UnEqual;
    short Index = 0;
    while ((int)Index < _LALR.Count & stateInfo.Index == -1)
    {
      if (State.ConfigSet.IsEqualBaseTo(_LALR[(int)Index].ConfigSet))
      {
        LRConfigCompare lrConfigCompare = State.ConfigSet.CompareCore(/*ref */_LALR[(int)Index].ConfigSet);
        stateInfo.Index = (int)Index;
        stateInfo.Compare = lrConfigCompare;
      }
      else
        checked { ++Index; }
    }
    return stateInfo;
  }

  private static void CheckGroupMissingStartEnd(BuilderGroupsList groups_, AppLog log_)
  {
    //TODO  здесь проблемка с енумерацией как GroupBuild т.к. енумератор определен у базового ParserGroupList
    //      и соответственно возвращает Group, а не GroupBuild
    //      Эту и многие пожожие проблемки нужно решать кардинально - уйти от прямого наследования от классов парсера
    //      и определить интерфейсы
    foreach (BuilderGroup group in groups_)
    {
      if (group.Start != null & group.End == null)
        log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Definition for '" + group.Name + " Start' is missing a matching '" + group.Name + " End'");
      else if (group.Start == null & group.End != null)
        log_.Add(AppLogSection.Grammar, AppLogAlert.Critical, "Definition for '" + group.Name + " End' is missing a matching '" + group.Name + " Start'");
    }
  }

  ////////

}

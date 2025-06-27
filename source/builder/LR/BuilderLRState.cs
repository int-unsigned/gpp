//
using System.Diagnostics;
using System.Xml.Linq;


//
//
namespace gpp.builder;


internal enum BuilderLRConflict
{
  None,
  ShiftReduce,
  ReduceReduce,
  AcceptReduce,
  AcceptShift,
}


//TODO
//LRActionType.Shift  -> lr_state_index                         -> используется в парсере для перехода в следующее состояние
//LRActionType.Goto   -> lr_state_index                         -> в парсере НЕ ИСПОЛЬЗУЕТСЯ
//LRActionType.Reduce -> .ParentProduction.TableIndex           -> в парсере используется для создания продукции
//LRActionType.Accept -> LRActionBuild.LR_ACTION_VALUE_DEFAULT  -> в парсере НЕ ИСПОЛЬЗУЕТСЯ
//TODO  Представляется возможным для парсера Type и Value схлопнуть в одно поле - >0 -> LrStateIndex, <0 -> ProductionIndex_
internal class BuilderLRAction(BuilderSymbol symbol_, LRActionType type_, int Value_LrStateIndex_Or_ProductionIndex_)
{
  public static int LR_ACTION_VALUE_DEFAULT = 0;  // ComputeLRState
  //
  public readonly BuilderSymbol   Symbol  = symbol_;
  public readonly LRActionType    Type    = type_;
  private readonly int            m_Value_LrStateIndex_Or_ProductionIndex_ = Value_LrStateIndex_Or_ProductionIndex_;   //TODO  именовать бы по человечески..
  //
  public int Value() => this.m_Value_LrStateIndex_Or_ProductionIndex_;
  public BuilderLRConflict ConflictWith(LRActionType other_lr_action_type_)
  {
    switch (this.Type)
    {
      case LRActionType.Shift:
        switch (other_lr_action_type_)
        {
          case LRActionType.Shift: return BuilderLRConflict.None;
          case LRActionType.Reduce: return BuilderLRConflict.ShiftReduce;
          case LRActionType.Accept: return BuilderLRConflict.AcceptShift;
        }
        break;
      case LRActionType.Reduce:
        switch (other_lr_action_type_)
        {
          case LRActionType.Shift: return BuilderLRConflict.ShiftReduce;
          case LRActionType.Reduce: return BuilderLRConflict.ReduceReduce;
          case LRActionType.Accept: return BuilderLRConflict.AcceptReduce;
        }
        break;
    }
    return BuilderLRConflict.None;
  }
  public string Name()
  {
    switch (this.Type)
    {
      case LRActionType.Shift:  return "Shift to State";
      case LRActionType.Reduce: return "Reduce Production";
      case LRActionType.Goto:   return "Go to State";
      case LRActionType.Accept: return "Accept";
      case LRActionType.Error:  return "Error";
    }
    Debug.Assert(false);
    return "!ParserLRAction Name Unexpected!";
  }
  public string NameShort()
  {
    switch (this.Type)
    {
      case LRActionType.Shift:  return "s";
      case LRActionType.Reduce: return "r";
      case LRActionType.Goto:   return "g";
      case LRActionType.Accept: return "a";
      case LRActionType.Error:  return "Error";
    }
    Debug.Assert(false);
    return "!ParserLRAction NameShort Unexpected!";
  }
  public string Text()
  {
    switch (this.Type)
    {
      case LRActionType.Shift:
      case LRActionType.Reduce:
      case LRActionType.Goto:     return this.Symbol.Text() + " " + this.Name() + " " + this.m_Value_LrStateIndex_Or_ProductionIndex_;
      default:                    return this.Symbol.Text() + " " + this.Name();
    }
  }
  public string TextShort()
  {
    switch (this.Type)
    {
      case LRActionType.Shift:
      case LRActionType.Reduce:
      case LRActionType.Goto:     return this.Symbol.Text() + " " + this.NameShort() + " " + this.m_Value_LrStateIndex_Or_ProductionIndex_;
      default:                    return this.Symbol.Text() + " " + this.NameShort();
    }
  }
}


//TODO  Используется только для BuilderLRState::PriorStates
//      то есть для BuilderView, а не построителя.
internal class BuilderLRStatesIndexSet
{
  private List<int> m_data;
  //
  public BuilderLRStatesIndexSet()
  {
    m_data = new();
  }
  //
  public void Add(int value_)
  {
    int insertion_cookie = m_data.BinarySearch(value_);
    if (insertion_cookie < 0)
      m_data.Insert(~insertion_cookie, value_);
  }
  public int Count()          => m_data.Count;
  public int this[int index_] => m_data[index_];
  public void Clear()         { m_data.Clear(); }
}


internal class BuilderLRState 
{
  public BuilderLRConfigSet           ConfigSet;
  public BuilderLRConflictItemsList   ConflictList;
  public string                       Note;
  public LRStatus                     Status;
  public BuilderLRStatesIndexSet      PriorStates;
  public bool                         Modified;
  public bool                         Expanded;
  private List<BuilderLRAction>       m_lr_actions;
  //
  public BuilderLRState() 
  {
    this.ConfigSet    = new BuilderLRConfigSet();
    this.ConflictList = new BuilderLRConflictItemsList();
    this.PriorStates  = new BuilderLRStatesIndexSet();
    this.Modified     = false;
    this.Expanded     = false;
    this.Note         = "";
    this.Status       = LRStatus.Info;
    m_lr_actions      = new List<BuilderLRAction>();
  }
  //
  public int CountOfLRActions()
  {
    return m_lr_actions.Count;
  }
  public BuilderLRAction this[int lr_action_index_]
  {
    get => m_lr_actions[lr_action_index_];
  }
  public void AddLRAction(BuilderLRAction Action) => m_lr_actions.Add(Action);


  //TODO  Интересно что возвращаемый LRConflict нигде не анализируется!
  //      СТРННЫЙ МЕТОД ПОЛУЧИЛСЯ !!!!
  //      - Если такой акции для симбола не было - создаем
  //      - Если такая акция для симбола была - тихо глотаем
  //      - Если такая акция была, но конфликтует - создаем, уведамляем о конфликте, НО РЕЗУЛЬТАТОМ НИКТО НЕ ИНТЕРЕСУЕТСЯ !!! 
  public BuilderLRConflict CreateActionIfAbsentOrIfConflict(BuilderSymbol lr_action_symbol_, LRActionType lr_action_type_, int lr_action_act_value_)
  {
    for (int lr_action_index = 0; lr_action_index < this.CountOfLRActions(); ++lr_action_index)
    {
      BuilderLRAction lrAction = this[lr_action_index];
      if (lrAction.Symbol.IsTheSame(lr_action_symbol_))
      {
        if (lrAction.Type == lr_action_type_ && lrAction.Value() == lr_action_act_value_)
          return BuilderLRConflict.None;
        else
        {
          this.AddLRAction(new BuilderLRAction(lr_action_symbol_, lr_action_type_, lr_action_act_value_));
          return lrAction.ConflictWith(lr_action_type_);
        }
      }
    }

    this.AddLRAction(new BuilderLRAction(lr_action_symbol_, lr_action_type_, lr_action_act_value_));
    return BuilderLRConflict.None;
  }

  public BuilderLRConflict CheckActionConflict(BuilderSymbol lr_action_symbol_, LRActionType lr_action_type_, int lr_action_act_value_)
  {
    for (int lr_action_index = 0; lr_action_index < this.CountOfLRActions(); ++lr_action_index)
    {
      BuilderLRAction lrAction = this[lr_action_index];
      if (lrAction.Symbol.IsTheSame(lr_action_symbol_))
      {
        if (lrAction.Type == lr_action_type_ && lrAction.Value() == lr_action_act_value_)
          return BuilderLRConflict.None;
        else
          lrAction.ConflictWith(lr_action_type_);
      }      
    }
    return BuilderLRConflict.None;
  }

}


internal class BuilderLRStatesList
{
  public short                                InitialState;
  protected readonly List<BuilderLRState>     m_data;
  //
  public BuilderLRStatesList()                => m_data = new List<BuilderLRState>();
  protected BuilderLRStatesList(int size_)    => m_data = new List<BuilderLRState>(size_);
  //
  public int Count                            => m_data.Count;
  public BuilderLRState this[int index_]      => m_data[index_];
  public int Add(BuilderLRState item_)
  {
    int i = m_data.Count;
    m_data.Add(item_);
    return i;
  }
}

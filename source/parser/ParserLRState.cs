//
using System.Diagnostics;



//
//
namespace gpp.parser;


internal class ParserLRAction
{
  private ParserSymbol        m_Symbol;
  private LRActionType  m_Type;
  private int           m_Value_LrStateTableIndex_Or_ActionValue;   //TODO  разобраться и именовать по человечески!
  //
  public ParserLRAction(ParserSymbol TheSymbol, LRActionType Type, int Value_LrStateTableIndex_Or_ActionValue)
  {
    this.m_Symbol   = TheSymbol;
    this.m_Type     = Type;
    this.m_Value_LrStateTableIndex_Or_ActionValue    = Value_LrStateTableIndex_Or_ActionValue;
  } 
  //
  public LRActionType Type()  => this.m_Type;
  public int Value()          => this.m_Value_LrStateTableIndex_Or_ActionValue;
  public ParserSymbol Symbol        => this.m_Symbol;
}



// LRState это фактически массив элементов ParserLRAction - структур указывающих "направление движения" парсера после считывания очередного симбола
internal class ParserLRState
{
  protected List<ParserLRAction> m_data;
  //
  //TODO  это вызывается из сериалайзера и теоретически должно иметь capacity
  protected ParserLRState() =>    m_data = new List<ParserLRAction>();
  //TODO  Вот здесь у нас плохо - цикл. Нужно думать!
  public int IndexOf(ParserSymbol symbol_)
  {
    for (int i = 0; i < m_data.Count; ++i)   
      if (symbol_.TableIndex == m_data[i].Symbol.TableIndex)
        return i;
    
    return -1;
  }
  public List<ParserLRAction>.Enumerator GetEnumerator()  => m_data.GetEnumerator();
  public ParserLRAction this[int lr_action_index_]        => m_data[lr_action_index_];
  public ParserLRAction? GetLRActionForSymbol(ParserSymbol symbol_)
  {
    int lr_action_index = this.IndexOf(symbol_);
    return (lr_action_index != -1)? m_data[lr_action_index] : null;
  }
}



internal class ParserLRStateList 
{
  protected List<ParserLRState> m_data;
  protected short         m_InitialState;
  //
  protected ParserLRStateList(int size_)  => m_data = new(size_);
  //
  public short InitialState         => m_InitialState;
  public bool IsEmpty()             => (m_data.Count == 0);
  public ParserLRState this[int index_]   => (ParserLRState)m_data[index_]; 
}



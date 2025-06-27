//
using System.Diagnostics;

//
//
namespace gpp.parser;



public class ParserSymbol
{
  public readonly string        Name;
  public readonly SymbolType    Type;
  public readonly short         TableIndex;
  protected       ParserGroup?  m_Group;
  //
  protected ParserSymbol(string name_, SymbolType type_, short table_index_)
  {
    this.Name       = name_;
    this.Type       = type_;
    this.TableIndex = table_index_;
  }
  public ParserGroup? Group => m_Group;
}


public class ParserSymbolList
{
  protected List<ParserSymbol>  m_Array;
  protected ParserSymbol        m_symbol_err = null;
  protected ParserSymbol        m_symbol_end = null;
  //
  protected ParserSymbolList(int capacity_)  => this.m_Array = new List<ParserSymbol>(capacity_);
  //
  public ParserSymbol this[int index_] => m_Array[index_];
  public ParserSymbol GetSymbol_End() 
  { 
    Debug.Assert (m_symbol_end != null);  //на момент вызова он уже должен быть установлен!
    return m_symbol_end;
  }
  public ParserSymbol GetSymbol_Err()
  {
    Debug.Assert(m_symbol_err != null);  //на момент вызова он уже должен быть установлен!
    return m_symbol_err;
  }
}

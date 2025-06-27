//
using System.Diagnostics;

//
//
namespace gpp.parser;

public class ParserGroup 
{
  public readonly table_index_t      TableIndex;
  public readonly string             Name;
  public readonly ParserSymbol       Container;
  public readonly ParserSymbol       Start;
  public readonly ParserSymbol       End;
  public readonly GroupAdvanceMode   Advance;
  public readonly GroupEndingMode    Ending;
  //TODO  Нужно предусмотреть какую-то опцию указывающую, что у нас не более 32 групп!
  protected AFX.BitMask32            m_NestingBmp;
  //
  internal ParserGroup(table_index_t table_index_, string name_, ParserSymbol container_, ParserSymbol start_symb_, ParserSymbol end_symb_, GroupAdvanceMode advance_mode_, GroupEndingMode ending_mode_)    
  {
    this.Name           = name_;
    this.Container      = container_;
    this.Start          = start_symb_;
    this.End            = end_symb_;
    this.Ending         = ending_mode_;
    this.Advance        = advance_mode_;
    this.TableIndex     = table_index_;
    this.m_NestingBmp   = default;
  }
  //
  public bool NestingContains(int value_) => m_NestingBmp.Get((uint)value_);
}


internal class ParserGroupList
{
  protected readonly List<ParserGroup> m_data;
  //
  protected ParserGroupList(int size_) => m_data = new List<ParserGroup>(size_);
}

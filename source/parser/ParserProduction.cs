//
using System.Diagnostics;

//
//
namespace gpp.parser;


//TODO  Парсеру вроде вообще ненужен сам список handle_symbols. Только кол-во сколько снимать со стека
//      Нужно обдумать это.
#if false //true

  public class ParserProduction
  {
    public readonly Symbol  HeadSymbol;
    public readonly short   TableIndex;
    protected List<Symbol>  m_handle_symbolset;
    //
    protected ParserProduction(Symbol head_symbol_, short table_index_)
    {
      this.HeadSymbol     = head_symbol_;
      this.TableIndex     = table_index_;
      m_handle_symbolset  = new List<Symbol>();
    }
    protected void AddHandleFromLoader(Symbol handle_symbol_)
    {
      m_handle_symbolset.Add(handle_symbol_);
    }
    public size_t HandleSymbolsCount()      => m_handle_symbolset.Count;
    internal bool ContainsOneNonTerminal()  => (this.m_handle_symbolset.Count== 1 && m_handle_symbolset[0].Type == SymbolType.Nonterminal);
  }

#else

public class ParserProduction
  {
    public readonly ParserSymbol  HeadSymbol;
    public readonly short         TableIndex;
    protected size_t              m_handle_symbolset_count;
    protected SymbolType          m_handle_symbolset_0_type;
    //
    protected ParserProduction(ParserSymbol head_symbol_, short table_index_)
    {
      this.HeadSymbol           = head_symbol_;
      this.TableIndex           = table_index_;
      m_handle_symbolset_count  = 0;
      m_handle_symbolset_0_type = default;
    }
    protected void AddHandleFromLoader(ParserSymbol handle_symbol_)
    {
      if (m_handle_symbolset_count == 0)
        m_handle_symbolset_0_type = handle_symbol_.Type;
      ++m_handle_symbolset_count;
    }

    public size_t HandleSymbolsCount()      => m_handle_symbolset_count;
    internal bool ContainsOneNonTerminal()  => (this.m_handle_symbolset_count == 1 && m_handle_symbolset_0_type == SymbolType.Nonterminal);
  }
//
#endif


public class ParserProductionList(size_t capacity_)
{
  protected List<ParserProduction> m_data   = new List<ParserProduction>(capacity_);
  //
  public ParserProduction this[int index_]  => m_data[index_];
}

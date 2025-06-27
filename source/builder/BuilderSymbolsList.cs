//
using System;
using System.Diagnostics;


//
//
namespace gpp.builder;


public enum SymbolCategory
{
  Nonterminal,
  Terminal,
  Special,
}



internal class BuilderSymbolsList 
{
  protected List<BuilderSymbol>   m_data;
  //TODO  Эти симболы при загурузке всегда первые, а при создании их тоже отдельно первыми создают
  //      соответственно нужно-бы их добавлять отдельными методами, а не утяжелять .Add условиями
  private readonly  BuilderSymbol    m_symbol_err;
  private readonly  BuilderSymbol    m_symbol_end;
  protected         BuilderSymbol    m_Symbol_NewLine  = null;
  //
  //
  protected BuilderSymbolsList(int size_, BuilderSymbol initial_symb_end_, BuilderSymbol initial_symb_err_)
  {
    m_data = new List<BuilderSymbol>(size_);
    Debug.Assert(initial_symb_end_.Type == SymbolType.End && initial_symb_end_.TableIndex == 0);
    m_symbol_end = initial_symb_end_;
    _internal_add(initial_symb_end_);
    Debug.Assert(initial_symb_err_.Type == SymbolType.Error && initial_symb_err_.TableIndex == 1);
    m_symbol_err = initial_symb_err_;
    _internal_add(initial_symb_err_);
  }
  //

  public size_t Count()                   => m_data.Count;
  public BuilderSymbol this[int index_]   => m_data[index_];  
  public BuilderSymbol GetSymbol_End()
  {
    return m_symbol_end;
  }

  protected BuilderSymbol _internal_add(BuilderSymbol item_)
  {
    m_data.Add(item_);
    return item_;
  }
}


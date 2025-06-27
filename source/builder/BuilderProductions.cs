//
using System.Diagnostics;

//
//
namespace gpp.builder;


internal class BuilderProductionHandleSymbolset
{
  protected List<BuilderSymbol> m_data;
  //
  public BuilderProductionHandleSymbolset()
  {
    m_data = new List<BuilderSymbol>();
  }
  //
  public void Add(BuilderSymbol symbol_)  => m_data.Add(symbol_);
  public int Count()                      => m_data.Count;
  public BuilderSymbol this[int index_]   => m_data[index_];
  public string ToStringDelimited(string delimiter_)
  { 
    return m_data.ToStringDelimited(delimiter_);
  }
}


internal class BuilderProduction 
{
  protected BuilderSymbol                   m_HeadSymbol;
  protected short                           m_TableIndex;
  private BuilderProductionHandleSymbolset  m_handle_symbols;
  //
  protected BuilderProduction(BuilderSymbol head_symbol_, short table_index_, BuilderProductionHandleSymbolset handle_symbols_)
  {
    m_HeadSymbol      = head_symbol_;
    m_TableIndex      = table_index_;
    m_handle_symbols  = handle_symbols_;
  }
  internal BuilderProduction(BuilderSymbol head_symbol_) : this(head_symbol_, -1, new BuilderProductionHandleSymbolset())
  { }
  //
  internal BuilderProductionHandleSymbolset Handle()    => m_handle_symbols;
  internal BuilderSymbol                    Head        => m_HeadSymbol; 
  public table_index_t                      TableIndex  => m_TableIndex;
  internal void SetTableIndex(short Value) 
  { 
    m_TableIndex = Value;
  }  
  internal string Name() => "<" + this.m_HeadSymbol.Name + ">";
  internal string Definition()
  {
    return m_handle_symbols.ToStringDelimited(" ");
  }
  public string Text()                  => this.Name() + " ::= " + this.Definition();
  public override string ToString()     => this.Text();
}


internal class BuilderProductionsList(size_t capacity_)
{
  protected List<BuilderProduction> m_data  = new List<BuilderProduction>(capacity_);
  //
  public BuilderProduction this[int index_] => m_data[index_];
  public void Add(BuilderProduction Item)   => m_data.Add(Item);
  public size_t Count()                     => m_data.Count;
}

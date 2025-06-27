//
using System.Diagnostics;

//
//
namespace gpp.builder;


internal class LRLookaheadSymbol : AFX.TArrayUniqueByKeyWithUnionItem<LRLookaheadSymbol>
{
  //
  public BuilderSymbol    ParentSymbol;
  public ConfigTrackSet   Configs;
  //
  public LRLookaheadSymbol(LRLookaheadSymbol other_lookahead_)
  {
    this.ParentSymbol = other_lookahead_.ParentSymbol;
    this.Configs      = new ConfigTrackSet(other_lookahead_.Configs);
  }
  public LRLookaheadSymbol(BuilderSymbol Sym)
  {
    this.ParentSymbol = Sym;
    this.Configs = new ConfigTrackSet();
  }
  //TArrayItemInterface
  public int CompareTo(LRLookaheadSymbol other_)
  {
    var my_key    = this.ParentSymbol.TableIndex;
    var other_key = other_.ParentSymbol.TableIndex;
    if (my_key < other_key) return -1;
    if (my_key > other_key) return 1;
    return 0;
  }
  public bool UnionWithOther(LRLookaheadSymbol other_lookahead_symbol_)
  {
    Debug.Assert(this.ParentSymbol.TableIndex == other_lookahead_symbol_.ParentSymbol.TableIndex);
    return this.Configs.UnionWith(other_lookahead_symbol_.Configs);
  }
}


internal class BuilderLRLookaheadSymbolSet
{
  private AFX.TArrayUniqueByKeyWithUnion<LRLookaheadSymbol> m_data;
  public BuilderLRLookaheadSymbolSet()
  { 
    m_data = new AFX.TArrayUniqueByKeyWithUnion<LRLookaheadSymbol> ();
  }
  public BuilderLRLookaheadSymbolSet(BuilderLRLookaheadSymbolSet other_)
  { 
    m_data = new AFX.TArrayUniqueByKeyWithUnion<LRLookaheadSymbol>(other_.m_data);
  }
  //
  public size_t Count()                                                 => m_data.Count();
  public LRLookaheadSymbol this[size_t index_]                          => m_data[index_];
  public void Add(LRLookaheadSymbol lookahead_symbol_)                  => m_data.Add(lookahead_symbol_);
  public bool UnionWith(BuilderLRLookaheadSymbolSet other_)             => m_data.UnionWith(other_.m_data);
  public void Clear() => m_data.Clear();

  // мы сортированный массив элементов LookaheadSymbol
  // уникальность и порядок сортировки задается LookaheadSymbol::ParentSymbol.TableIndex
  // когда нас сравнивают с Другим, то мы можем быть .Equal, .UnEqual, .Subset
  public AFX.SetCompareEnum CompareToCore(BuilderLRLookaheadSymbolSet other_set_)
  {
    int my_count    = this.Count();
    int other_count = other_set_.Count();

    if (my_count == other_count)
    {
      for (int i = 0; i < my_count; ++i)        
        if (this[i].CompareTo(other_set_[i]) != 0)
          return AFX.SetCompareEnum.UnEqual;
      return AFX.SetCompareEnum.Equal;
    }
    else if (my_count < other_count)
    {
      int my_index = 0;
      int other_index = 0;
      while (my_index < my_count && other_index < other_count)
      {
        if (this[my_index].CompareTo(other_set_[other_index]) == 0)
        {
          ++my_index;
          ++other_index;
        }
        else
          ++other_index;
      }
      return (my_index < my_count)? AFX.SetCompareEnum.UnEqual : AFX.SetCompareEnum.Subset;
    }

    return AFX.SetCompareEnum.UnEqual;
  }

}



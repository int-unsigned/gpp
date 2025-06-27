//
using System.Diagnostics;

//
#nullable disable

//
//
namespace gpp.builder;


internal enum LRConfigCompare
{
  ProperSubset,
  EqualFull,
  EqualBaseNotEqualLookahead,
  UnEqual,
}

internal enum LRStatus : byte
{
  None,
  Info,
  Warning,
  Critical,
}


internal class LRConfig : AFX.TArrayUniqueByKeyWithUnionItem<LRConfig>
{
  private int               m_Position;
  public BuilderLRLookaheadSymbolSet LookaheadSet;
  public bool               Modified;
  public bool               InheritLookahead;
  private BuilderProduction   m_ParentProduction;
  public LRStatus           Status;
  //
  private int               m_hash;
  //
  public BuilderProduction    ParentProduction  => m_ParentProduction;
  public int                Position          => m_Position;
  //
  private LRConfig(BuilderProduction parent_production_, int position_, bool modified_, bool inherit_lookahead_, BuilderLRLookaheadSymbolSet lookahead_symbol_set_)
  {
    this.m_ParentProduction   = parent_production_;
    this.m_Position           = position_;
    this.Modified             = modified_;
    this.InheritLookahead     = inherit_lookahead_;
    this.LookaheadSet         = lookahead_symbol_set_;
    this.Status               = default;
    //
    this.m_hash               = HashCode.Combine(parent_production_.TableIndex, position_);
  }
  public LRConfig(BuilderProduction parent_production_, int position_, BuilderLRLookaheadSymbolSet init_lookahead_symbol_set_)
    : this(parent_production_, position_, modified_: true, inherit_lookahead_: false, new BuilderLRLookaheadSymbolSet(init_lookahead_symbol_set_))
  { }
  public LRConfig(BuilderProduction parent_production_, int position_, bool modified_, bool inherit_lookahead_)
    : this(parent_production_, position_, modified_, inherit_lookahead_, new BuilderLRLookaheadSymbolSet())
  { }
  public LRConfig(BuilderProduction parent_production_)
    : this(parent_production_, position_: 0, modified_: true, inherit_lookahead_: false, new BuilderLRLookaheadSymbolSet())
  { }
  //
  public int GetHash()      => m_hash;
  //
  public int TableIndex()   => (int)this.ParentProduction.TableIndex;
  public bool IsComplete()  => (this.Position > (this.ParentProduction.Handle().Count() - 1));
  public LRActionType NextAction()
  {
    if (this.Position < this.ParentProduction.Handle().Count())
      return (this.NextSymbol().Category() != SymbolCategory.Terminal) ? LRActionType.Goto : LRActionType.Shift;
    else
      return LRActionType.Reduce;
  }
  

  public BuilderSymbol? NextSymbol(int offset_ = 0)
  {
    Debug.Assert(offset_ >= 0);
    if ((this.Position + offset_) < this.ParentProduction.Handle().Count())
      return this.ParentProduction.Handle()[this.Position + offset_];
    else
      return null;
  }
  public BuilderSymbol Checkahead(int offset_ /*= 0*/)
  {
    return (this.Position <= (this.ParentProduction.Handle().Count() - 1 -  offset_)) ? 
      this.ParentProduction.Handle()[this.Position + 1 + offset_] : (BuilderSymbol) null;
  }
  public short CheckaheadCount()
  {
    return checked((short)(this.ParentProduction.Handle().Count() - (int)this.Position - 1));
  }


  // это порядок сортировки для LRConfigSet (TArrayUniqueByKeyWithUnion<LRConfig>)
  public int CompareTo(LRConfig other_config_)
  {
    int cmp_table_index = this.TableIndex().CompareTo(other_config_.TableIndex());
    if (cmp_table_index == 0)
      return this.Position.CompareTo(other_config_.Position);
    else
      return cmp_table_index;
  }
  public bool UnionWithOther(LRConfig other_item_)
  {
    bool SetChanged;
    if (this.LookaheadSet.UnionWith(other_item_.LookaheadSet))
    {
      this.Modified = true;
      SetChanged = true;
    }
    else
    {
      this.Modified = this.Modified | other_item_.Modified;
      SetChanged = false;
    }
    return SetChanged;
  }


  // это сравнение НЕ учитывает прочие данные (в частности LookaheadSet). Только данные "ключа сортировки".
  public bool IsEqualKeyTo(LRConfig other_config_)
  {
    return this.ParentProduction.TableIndex == other_config_.TableIndex() && this.Position == other_config_.Position;
  }

  // это сравнение с учетом .LookaheadSet для алгоритмов построителя
  public LRConfigCompare CompareCore(LRConfig ConfigB)
  {
    if (this.ParentProduction.TableIndex == ConfigB.TableIndex() && this.Position == (int)ConfigB.Position)
    {
      var cmp_lookahead = this.LookaheadSet.CompareToCore(ConfigB.LookaheadSet);
      switch (cmp_lookahead)
      {
        case AFX.SetCompareEnum.Equal:      return LRConfigCompare.EqualFull;
        // возвращается LRConfigCompare.EqualBaseNotEqualLookahead если .TableIndex и .ParserPosition равны, но .LookaheadSet никак не равен (.UnEqual)
        case AFX.SetCompareEnum.UnEqual:    return LRConfigCompare.EqualBaseNotEqualLookahead;
        default:    //case DictionarySet.Compare.Subset: return LRConfigCompare.ProperSubset;
          Debug.Assert(cmp_lookahead == AFX.SetCompareEnum.Subset);
          return LRConfigCompare.ProperSubset;
      }
    }
    else
      return LRConfigCompare.UnEqual;
  }

}

//

//
//
namespace gpp.builder;


//TODO  Назначение этого класса я непонял..
internal class BuilderLRConflictItem
{
  public BuilderSymbol       Symbol;
  public BuilderLRConflict   Conflict;
  public BuilderLRConfigSet  Reduces;
  public BuilderLRConfigSet  Shifts;

  public BuilderLRConflictItem(BuilderSymbol symbol_)
  {
    this.Symbol     = symbol_;
    this.Conflict   = BuilderLRConflict.None;
    this.Shifts     = new BuilderLRConfigSet();
    this.Reduces    = new BuilderLRConfigSet();
  }
  public BuilderLRConflictItem(BuilderLRConflictItem other_item_, BuilderLRConflict lr_conflict_status_)
  {
    this.Symbol     = other_item_.Symbol;
    this.Conflict   = lr_conflict_status_;
    this.Shifts     = other_item_.Shifts;
    this.Reduces    = other_item_.Reduces;
  }
}


internal class BuilderLRConflictItemsList
{
  private List<BuilderLRConflictItem> m_data;
  //
  public BuilderLRConflictItemsList()
  {
    m_data = new List<BuilderLRConflictItem>();
  }
  //
  internal void Add(BuilderLRConflictItem item_) => m_data.Add(item_);
  internal BuilderLRConflictItem this[int index_]
  {
    get => (BuilderLRConflictItem)m_data[index_];
  }
}

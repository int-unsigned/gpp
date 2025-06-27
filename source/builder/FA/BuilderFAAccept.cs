//
using System.Diagnostics;

//
//
namespace gpp.builder;


internal class BuilderFAAccept(short symbol_index_, short priority_)
{
  private readonly short m_SymbolIndex = symbol_index_;
  private readonly short m_Priority    = priority_;
  //
  public short SymbolIndex  => this.m_SymbolIndex;
  public short Priority     => this.m_Priority;    
}


internal class BuilderFAAcceptList
{
  private List<BuilderFAAccept> m_data;
  //
  public BuilderFAAcceptList() 
  { 
    m_data = new List<BuilderFAAccept>();
  } 
  public void Clear()   => this.m_data.Clear();
  public int Count()    => this.m_data.Count;
  public BuilderFAAccept this[int index_]
  {
    get
    {
      Debug.Assert(index_ >= 0 && index_ < this.m_data.Count);
      return this.m_data[index_];
    }
  }

  //TODO  Как бы желательно либо всегда принимать элементы, либо всегда создавать их
  //      Нужно разобраться в вызывающих..
  public void Add(BuilderFAAccept item_) => this.m_data.Add(item_);
  public void AddNewFAAccept(short symbol_index_, short priority_)
  {
     this.m_data.Add(new BuilderFAAccept(symbol_index_, priority_));
  }
}

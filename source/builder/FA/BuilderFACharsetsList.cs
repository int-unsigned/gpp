//
using System.Diagnostics;

//
//
namespace gpp.builder;


internal class BuilderFACharsetsList 
{
  protected List<BuilderFACharset> m_data;
  //
  protected BuilderFACharsetsList(int size_)
  {
    m_data = new List<BuilderFACharset>(size_);
  }
  public BuilderFACharsetsList()
    :this(0)
  { }
  //
  public BuilderFACharset this[int index_]   { get => m_data[index_]; }
  public size_t Count()                       => m_data.Count;
  public void AddAndSetTableIndex(BuilderFACharset item_)
  {
    Debug.Assert(item_.TableIndex < 0);
    int item_index = m_data.Count();
    item_.SetTableIndex(item_index);
    m_data.Add(item_);
  }
}


//

//
//
namespace gpp.builder;


//TODO  Это внутренний класс для LR-построителя
//      ранее было реализовано как сортед лист. сделал HashSet особо ничего не поменялось.. 
//      хз.. может хэш хреново делаю (см. LRConfigSet.UnionWith) или вызывающего оптимизировать (см. BuildLR.CreateLRState)
//        .. или больше здесь ничего не выдавишь..
//
//      Для памяти - КЛЮЧЕМ контейнера является МАССИВ - это не соысем просто..
//
// LRConfigSetLookup это фактически хэш-таблица, ключами в которой являются элементы LRConfigSet
//  LRConfigSet - это массив элементов LRConfig, уникальность которых определяется 
//    ParentProduction(Production)_TableIndex и .ParserPosition (или наоборот .ParserPosition потом _TableIndex - там запутано..)
//      ParentProduction(Production)_TableIndex назначается один раз в самом начале в методе BuilderApp.AssignTableIndexes и принимает значения 0-BuildTables.Production.Count()-1
//      int ParserPosition назначается при создании объекта LRConfig, далее не меняется, и принимает значения 0...+1... в общем немного..


using System.Diagnostics;

internal sealed class BuilderLRConfigSetLookup
{
  private struct LRConfigSetLookupItem
  {
    public BuilderLRConfigSet   Key;
    public int                  TableIndex;
  }
  private class ConfigSetLookupItemEqualityComparer : IEqualityComparer<LRConfigSetLookupItem>
  {   
    bool IEqualityComparer<LRConfigSetLookupItem>.Equals(LRConfigSetLookupItem x, LRConfigSetLookupItem y)    => BuilderLRConfigSet.IsEqualBase(x.Key, y.Key);
    int IEqualityComparer<LRConfigSetLookupItem>.GetHashCode(LRConfigSetLookupItem item_)                     => item_.Key.GetHash();
    //
    public static readonly ConfigSetLookupItemEqualityComparer Instance = new();
  }
  //
  private HashSet<LRConfigSetLookupItem> m_List;
  //
  public BuilderLRConfigSetLookup()     => m_List = new(ConfigSetLookupItemEqualityComparer.Instance);
  //  
  public void Add(BuilderLRConfigSet Key, int TableIndex)
  {
    LRConfigSetLookupItem item;
    item.Key = Key;
    item.TableIndex = TableIndex;
    bool b = m_List.Add(item);
    //TODO  Нас вызывают в алгоритме "если НЕ get_TableIndex, то Add"
    //      соответственно мы не можем не-Add
    //      а вообще нужно Хэш нормальный сделать, который бы букет возвращал в который .Add делать надо, если ненайдено
    //      чтобы хэш два раза не гонять
    //TODO  И вообще здесь нужен не их HashSet, а мой THashSetEx, т.к. поиск идет не по итему, а по ключу.
    //      только его для структур доработать надо.. он сейчас для классов..
    Debug.Assert(b);
  }
  public int get_TableIndex(BuilderLRConfigSet Key)
  {
    LRConfigSetLookupItem item_key;
    item_key.Key        = Key;
    item_key.TableIndex = default;
    if(m_List.TryGetValue(item_key, out LRConfigSetLookupItem item_value))
      return item_value.TableIndex;
    else
      return -1;
  }
}

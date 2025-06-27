//
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

//
//
namespace gpp.builder;


internal enum ConfigTrackSource
{
  Inherit,
  Config,
  First,
}


internal class LRConfigTrack : AFX.TArrayUniqueByKeyWithUnionItem<LRConfigTrack> 
{
  public LRConfig Parent;
  public bool     FromConfig;
  public bool     FromFirst;
  //
  public LRConfigTrack(LRConfig Config, ConfigTrackSource Source)
  {
    this.Parent = Config;
    this.FromConfig = (Source == ConfigTrackSource.Config);
    this.FromFirst  = (Source == ConfigTrackSource.First);
  }
  //TArrayItemInterface
  public int CompareTo([DisallowNull] LRConfigTrack other_)
  {
    Debug.Assert(other_ != null);
    var my_key    = this.Parent.TableIndex();
    var it_key    = other_.Parent.TableIndex();
    if (my_key < it_key) return -1;
    if (my_key > it_key) return 1;
    return 0;
  }
  public bool UnionWithOther(LRConfigTrack other_)
  {
    this.FromConfig = (this.FromConfig | other_.FromConfig);
    this.FromFirst  = (this.FromFirst | other_.FromFirst);
    //TODO  Как и у оригинала мы не устанавливаем "DictionarySet.MemberResult.SetChanged"
    return false;
  }
}


internal class ConfigTrackSet : AFX.TArrayUniqueByKeyWithUnion<LRConfigTrack>
{
  public ConfigTrackSet() : base()
  { }
  public ConfigTrackSet(ConfigTrackSet other_) : base(other_)
  { }
}

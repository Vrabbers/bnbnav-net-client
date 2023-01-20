using System.Collections.Generic;
using BnbnavNetClient.Models;

namespace BnbnavNetClient.ViewModels;
public sealed class NewEdgeFlyoutViewModel : ViewModel
{
    public List<Node> NodesToJoin { get; set; }
}

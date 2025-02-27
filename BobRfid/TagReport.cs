using System.Collections.Generic;

namespace BobRfid
{
    public class TagReport
    {
        private List<Tag> _tags = new List<Tag>();

        public List<Tag> Tags { get => _tags; }
    }
}
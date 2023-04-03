using System.ComponentModel.DataAnnotations;

namespace Sample.Sdk.Data
{
    public class Message
    {
        private string _id;
        [Key]
        public string Id
        {
            get
            {
                if (string.IsNullOrEmpty(_id))
                {
                    _id = Guid.NewGuid().ToString();
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }
    }
}


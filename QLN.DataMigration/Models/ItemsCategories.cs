using System.Text.Json.Serialization;

namespace QLN.DataMigration.Models
{
    public class Model
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("makeId")]
        public int MakeId { get; set; }

        [JsonPropertyName("makeName")]
        public string MakeName { get; set; }
    }

    public class ItemsCategories
    {
        [JsonPropertyName("models")]
        public List<Model> Models { get; set; }

        public static explicit operator ItemsCategories(DrupalItemsCategories itemsCategories)
        {
            var models = new List<Model>();

            foreach (var model in itemsCategories.Makes)
            {
                foreach(var item in model.Models)
                {
                    var newModel = new Model
                    {
                        Id = item.Id,
                        Name = item.Name,
                        MakeId = model.Id,
                        MakeName = model.Name
                    };
                    models.Add(newModel);
                }
            }

            return new ItemsCategories
            {
                Models = models
            };
        }
    }
}

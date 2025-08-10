using QLN.Common.Infrastructure.DTO_s;

namespace QLN.DataMigration.Models
{
    public static class Constants
    {
        public const string CategoriesEndpoint = "http://ql-migrate.westeurope.cloudapp.azure.com/qlnext-v2/items-categories.php";
        public const string ItemsEndpoint = "http://ql-migrate.westeurope.cloudapp.azure.com/qlnext-v2/items.php";

        public static List<Common.DTO_s.LocationDto.LocationEventDto> Zones()
        {
            return new List<Common.DTO_s.LocationDto.LocationEventDto>
            {
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "126226",
                    Name = "Izghawa",
                    Latitude = "25.3598",
                    Longitude = "51.4362",
                    Areas = null
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "114911",
                    Name = "Al Khartiyat",
                    Latitude = "25.3861",
                    Longitude = "51.4257",
                    Areas = null
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "114971",
                    Name = "Dukhan",
                    Latitude = "25.428",
                    Longitude = "50.7833",
                    Areas = null
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "102946",
                    Name = "Wakrah",
                    Latitude = "25.1659",
                    Longitude = "51.5976",
                    Areas = new List<Common.DTO_s.LocationDto.AreaDto>
                    {
                        new Common.DTO_s.LocationDto.AreaDto
                        {
                            Id = "115581",
                            Name = "Al Wukair",
                            Latitude = "25.1561",
                            Longitude = "51.5477"
                        },
                        new Common.DTO_s.LocationDto.AreaDto
                        {
                            Id = "126176",
                            Name = "Birkat Al Awamer",
                            Latitude = "25.0747",
                            Longitude = "51.4914"
                        }
                    }
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "114526",
                    Name = "Mesaeidd",
                    Latitude = "24.9909",
                    Longitude = "51.5493",
                    Areas = null
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "114906",
                    Name = "Al Kheesa / Al Ebb",
                    Latitude = "25.3913",
                    Longitude = "51.4493",
                    Areas = null
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "102696",
                    Name = "Doha",
                    Latitude = "25.2854",
                    Longitude = "51.531",
                    Areas = new List<Common.DTO_s.LocationDto.AreaDto>
                    {
                        new Common.DTO_s.LocationDto.AreaDto { Id = "106626", Name = "Abu Hamour", Latitude = "25.2388", Longitude = "51.4914" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "103406", Name = "Ain Khaled", Latitude = "25.2258", Longitude = "51.4572" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "114886", Name = "Al Aziziya", Latitude = "25.2426", Longitude = "51.4467" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102701", Name = "Al Bidda", Latitude = "25.2919", Longitude = "51.5216" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "125791", Name = "Al Corniche", Latitude = "25.3144", Longitude = "51.5216" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "126169", Name = "Al Daayen", Latitude = "25.5101", Longitude = "51.3369" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102706", Name = "Al Dafna", Latitude = "25.3077", Longitude = "51.5163" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102711", Name = "Al Doha Al Jadeeda", Latitude = "25.2776", Longitude = "51.5321" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102716", Name = "Al Duhail", Latitude = "25.3477", Longitude = "51.4675" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102721", Name = "Al Gharrafa", Latitude = "25.3326", Longitude = "51.4467" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102726", Name = "Al Hilal", Latitude = "25.2599", Longitude = "51.5439" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102731", Name = "Al Jasra", Latitude = "25.2884", Longitude = "51.5327" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102736", Name = "Al Jebailat", Latitude = "25.3212", Longitude = "51.5032" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102881", Name = "Al Khulaifat", Latitude = "25.2839", Longitude = "51.561" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102746", Name = "Al Khuwair", Latitude = "25.3136", Longitude = "51.5111" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102751", Name = "Al Luqta / Old Al Rayyan", Latitude = "25.3033", Longitude = "51.4511" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "106636", Name = "Al Maamoura", Latitude = "25.247", Longitude = "51.5019" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102756", Name = "Al Mansoura / Fereej Bin Dirham", Latitude = "25.2686", Longitude = "51.53" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102761", Name = "Al Markhiya", Latitude = "25.3388", Longitude = "51.4992" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "106641", Name = "Al Mesaimeer", Latitude = "25.2348", Longitude = "51.5092" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102766", Name = "Al Messila", Latitude = "25.3006", Longitude = "51.4809" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "118016", Name = "Al Mirqab", Latitude = "25.2811", Longitude = "51.535" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102361", Name = "Al Muntazah", Latitude = "25.2719", Longitude = "51.5176" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102771", Name = "Al Najada", Latitude = "25.2826", Longitude = "51.5341" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "115271", Name = "Al Nasr", Latitude = "25.2696", Longitude = "51.4979" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102776", Name = "Al Qassar", Latitude = "25.3513", Longitude = "51.5265" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102781", Name = "Al Qutaifiya", Latitude = "25.3595", Longitude = "51.5012" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102786", Name = "Al Rumaila", Latitude = "25.2964", Longitude = "51.5164" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102791", Name = "Al Sadd", Latitude = "25.2838", Longitude = "51.4914" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102801", Name = "Al Tarfa / Jelaiah", Latitude = "25.3522", Longitude = "51.4861" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102806", Name = "Al Thumama", Latitude = "25.2316", Longitude = "51.5413" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102811", Name = "Al Waab / Al Aziziya / New Al Ghanim", Latitude = "25.2591", Longitude = "51.4677" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "118166", Name = "Al-Sailiya", Latitude = "25.2159", Longitude = "51.3547" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "109941", Name = "Aspire Zone", Latitude = "25.2637", Longitude = "51.4409" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "125792", Name = "Bani Hajer", Latitude = "25.3163", Longitude = "51.4021" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "106631", Name = "Barwa City", Latitude = "25.1986", Longitude = "51.5071" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "109821", Name = "Barwa Village", Latitude = "25.212", Longitude = "51.5794" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "126203", Name = "Diplomatic Area", Latitude = "25.3308", Longitude = "51.5275" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102816", Name = "Doha International Airport", Latitude = "25.2609", Longitude = "51.6138" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102821", Name = "Doha Port", Latitude = "25.295", Longitude = "51.5439" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "109936", Name = "Education City", Latitude = "25.3141", Longitude = "51.4415" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102826", Name = "Fareej Al Ali", Latitude = "25.2485", Longitude = "51.519" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102831", Name = "Fereej Abdel Aziz", Latitude = "25.2777", Longitude = "51.5242" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102836", Name = "Fereej Al Ameer / Muraikh", Latitude = "25.2874", Longitude = "51.4703" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "125794", Name = "Fereej Al Murra", Latitude = "25.232", Longitude = "51.4368" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "125793", Name = "Fereej Al Soudan", Latitude = "25.2672", Longitude = "51.4861" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102841", Name = "Fereej Bin Mahmoud", Latitude = "25.2803", Longitude = "51.5124" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102846", Name = "Fereej Bin Omran", Latitude = "25.3038", Longitude = "51.4953" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102851", Name = "Fereej Kulaib", Latitude = "25.3138", Longitude = "51.4914" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102856", Name = "Industrial Area", Latitude = "25.2029", Longitude = "51.4349" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "109926", Name = "Katara Cultural Village", Latitude = "25.361", Longitude = "51.5245" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "126204", Name = "Legtaifiya", Latitude = "25.3645", Longitude = "51.5104" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "118021", Name = "Luaib", Latitude = "25.2841", Longitude = "51.4598" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "114981", Name = "LUSAIL", Latitude = "25.4254", Longitude = "51.5045" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102861", Name = "Madinat Khalifa North / Dahl Al Hamam", Latitude = "25.3337", Longitude = "51.4701" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102866", Name = "Madinat Khalifa South", Latitude = "25.3156", Longitude = "51.4809" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "118011", Name = "Mehairja", Latitude = "25.2691", Longitude = "51.4598" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "115631", Name = "Muither", Latitude = "25.2629", Longitude = "51.4152" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102871", Name = "Mushaireb", Latitude = "25.2843", Longitude = "51.5216" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102876", Name = "Najma", Latitude = "25.2683", Longitude = "51.5387" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "114891", Name = "New Al Ghanim", Latitude = "25.2358", Longitude = "51.4439" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "125790", Name = "New Al Hitmi", Latitude = "25.2849", Longitude = "51.5479" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102886", Name = "New Al Rayyan / Al Wajba", Latitude = "25.2932", Longitude = "51.3837" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "125795", Name = "New Industrial Area", Latitude = "25.1702", Longitude = "51.4164" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102891", Name = "New Salata / Al Asiri", Latitude = "25.2602", Longitude = "51.5163" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102896", Name = "Nuaija", Latitude = "25.2467", Longitude = "51.5334" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102901", Name = "Old Airport", Latitude = "25.2481", Longitude = "51.5544" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102906", Name = "Old Al Ghanim", Latitude = "25.28", Longitude = "51.54" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102911", Name = "Old Al Hitmi", Latitude = "25.2847", Longitude = "51.5503" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102916", Name = "Old Salata", Latitude = "25.2846", Longitude = "51.5463" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102921", Name = "Onaiza", Latitude = "25.3469", Longitude = "51.5176" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102951", Name = "Other", Latitude = "25.3548", Longitude = "51.1839" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "109931", Name = "Qatar National Convention Center", Latitude = "25.3221", Longitude = "51.4374" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "126231", Name = "Ras Abu Abboud", Latitude = "25.2857", Longitude = "51.5736" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102926", Name = "Rawdat Al Khail", Latitude = "25.27", Longitude = "51.5207" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102796", Name = "Souq Waqif", Latitude = "25.2884", Longitude = "51.5306" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "109816", Name = "The Pearl Qatar", Latitude = "25.3686", Longitude = "51.5516" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "126171", Name = "Umm Abirieh", Latitude = "25.3149", Longitude = "51.3759" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "125659", Name = "Umm Al Amad", Latitude = "25.4896", Longitude = "51.3968" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "125965", Name = "Umm Al Seneem", Latitude = "25.223", Longitude = "51.4691" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "126220", Name = "Umm Ebairiya", Latitude = "25.493", Longitude = "51.3759" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102931", Name = "Umm Ghwailina", Latitude = "25.2766", Longitude = "51.5492" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102936", Name = "Umm Lekhba", Latitude = "25.3477", Longitude = "51.4675" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "126170", Name = "Umm Qarn", Latitude = "25.3149", Longitude = "51.4382" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "115266", Name = "Umm Salal Ali", Latitude = "25.4696", Longitude = "51.3968" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "115301", Name = "Umsalal Mohammed", Latitude = "25.3984", Longitude = "51.4247" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "126191", Name = "Wadi Aba Saleel", Latitude = "25.14", Longitude = "51.46" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "102941", Name = "Wadi Al Sail", Latitude = "25.3087", Longitude = "51.5071" },
                        new Common.DTO_s.LocationDto.AreaDto { Id = "103401", Name = "West Bay", Latitude = "25.3218", Longitude = "51.5334" }
                    }
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "124471",
                    Name = "Al-Shahaniya",
                    Latitude = "25.4106",
                    Longitude = "51.1846",
                    Areas = new List<Common.DTO_s.LocationDto.AreaDto>
                    {
                        new Common.DTO_s.LocationDto.AreaDto
                        {
                            Id = "126175",
                            Name = "Abu Nakhla",
                            Latitude = "25.3149",
                            Longitude = "51.246"
                        }
                    }
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "114901",
                    Name = "Al Sakhama",
                    Latitude = "25.4728",
                    Longitude = "51.4152",
                    Areas = null
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "126023",
                    Name = "Al Ruwais / Madinat Al Shamal",
                    Latitude = "26.1301",
                    Longitude = "51.1978",
                    Areas = null
                },
                new Common.DTO_s.LocationDto.LocationEventDto
                {
                    Id = "102741",
                    Name = "Al Khor",
                    Latitude = "25.6804",
                    Longitude = "51.4968",
                    Areas = new List<Common.DTO_s.LocationDto.AreaDto>
                    {
                        new Common.DTO_s.LocationDto.AreaDto
                        {
                            Id = "125619",
                            Name = "Al Dhakhira / Al Thakhira",
                            Latitude = "25.7342",
                            Longitude = "51.5432"
                        },
                        new Common.DTO_s.LocationDto.AreaDto
                        {
                            Id = "124101",
                            Name = "Simaisma",
                            Latitude = "25.5756",
                            Longitude = "51.4809"
                        }
                    }
                }
            };
        }

        public static List<NewsCategory> NewsCategories()
        {
            return new List<NewsCategory>
            {
                new NewsCategory
                {
                    Id = 101,
                    CategoryName = "News",
                    SubCategories = new List<NewsSubCategory>
                    {
                        new NewsSubCategory { Id = 1001, SubCategoryName = "Qatar" },
                        new NewsSubCategory { Id = 1002, SubCategoryName = "Middle East" },
                        new NewsSubCategory { Id = 1003, SubCategoryName = "World" },
                        new NewsSubCategory { Id = 1004, SubCategoryName = "Health & Education" },
                        new NewsSubCategory { Id = 1005, SubCategoryName = "Community" },
                        new NewsSubCategory { Id = 1006, SubCategoryName = "Law" }
                    }
                },
                new NewsCategory
                {
                    Id = 102,
                    CategoryName = "Business",
                    SubCategories = new List<NewsSubCategory>
                    {
                        new NewsSubCategory { Id = 1007, SubCategoryName = "Qatar Economy" },
                        new NewsSubCategory { Id = 1008, SubCategoryName = "Market Updates" },
                        new NewsSubCategory { Id = 1009, SubCategoryName = "Real Estate" },
                        new NewsSubCategory { Id = 1010, SubCategoryName = "Entrepreneurship" },
                        new NewsSubCategory { Id = 1011, SubCategoryName = "Finance" },
                        new NewsSubCategory { Id = 1012, SubCategoryName = "Jobs & Careers" }
                    }
                },
                new NewsCategory
                {
                    Id = 103,
                    CategoryName = "Sports",
                    SubCategories = new List<NewsSubCategory>
                    {
                        new NewsSubCategory { Id = 1013, SubCategoryName = "Qatar Sportss" },
                        new NewsSubCategory { Id = 1014, SubCategoryName = "Football" },
                        new NewsSubCategory { Id = 1015, SubCategoryName = "International" },
                        new NewsSubCategory { Id = 1016, SubCategoryName = "Motorsports" },
                        new NewsSubCategory { Id = 1017, SubCategoryName = "Olympics" },
                        new NewsSubCategory { Id = 1018, SubCategoryName = "Athlete Features" }
                    }
                },
                new NewsCategory
                {
                    Id = 104,
                    CategoryName = "Lifestyle",
                    SubCategories = new List<NewsSubCategory>
                    {
                        new NewsSubCategory { Id = 1019, SubCategoryName = "Food and Dining" },
                        new NewsSubCategory { Id = 1020, SubCategoryName = "Travel & Leisure" },
                        new NewsSubCategory { Id = 1021, SubCategoryName = "Arts and Culture" },
                        new NewsSubCategory { Id = 1022, SubCategoryName = "Events" },
                        new NewsSubCategory { Id = 1023, SubCategoryName = "Fashion & Style" },
                        new NewsSubCategory { Id = 1024, SubCategoryName = "Home & Living" }
                    }
                }
            };
        }
    }
}

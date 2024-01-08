using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Runtime;
using System.Text;
using Tp_CourseWork.GofComand;
using Tp_CourseWork.Models;
using Tp_CourseWork.Models.ViewModels;

namespace Tp_CourseWork.Controllers
{
    public class HomeController : Controller
    {
        private List<Statistic> ConvertJArrayToStatistics(JArray array)
        {
            var statisticsList = new List<Statistic>();

            foreach (var item in array)
            {
                var category = item["Name"]?.ToString();
                var median = item["Median"]?.Value<double>() ?? 0;
                var mean = item["Mean"]?.Value<double>() ?? 0;
                var max = item["Max"]?.Value<double>() ?? 0;
                var min = item["Min"]?.Value<double>() ?? 0;

                statisticsList.Add(new Statistic
                {
                    Name = category,
                    Median = median,
                    Mean = mean,
                    Max = max,
                    Min = min
                });
            }

            return statisticsList;
        }

        //Hosted web API REST Service base url
        string Baseurl = "https://localhost:49105/";
        public async Task<ActionResult> Index()
        {

            List<Locality> LocInfo = new List<Locality>();
            List<string> Mayors = new List<string>();
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetLocalities using HttpClient
                HttpResponseMessage Res = await client.GetAsync("/api/GetLocalities");
                //Checking the response is successful or not which is sent using HttpClient
                if (Res.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api
                    var LocResponse = Res.Content.ReadAsStringAsync().Result;
                    //Deserializing the response recieved from web api and storing into the Localities list
                    LocInfo = JsonConvert.DeserializeObject<List<Locality>>(LocResponse);
                }

                for (int i = 0; i < LocInfo.Count; i++)
                {
                    Mayors.Add(LocInfo[i].Mayor);
                }

                Mayors = Mayors.Distinct().ToList();

                var model = new LocalitiesWithUniqMayor
                {
                    Localities = LocInfo,
                    Mayors = Mayors
                };

                //returning the Localities list to view
                return View(model);
            }
        }

        public async Task<ActionResult> Details(int id)
        {
            Locality LocInfo = null;
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetLocalities using HttpClient
                HttpResponseMessage Res = await client.GetAsync("api/GetLocalitiyById?id=" + id.ToString());
                //Checking the response is successful or not which is sent using HttpClient
                if (Res.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api
                    var LocResponse = Res.Content.ReadAsStringAsync().Result;
                    //Deserializing the response recieved from web api and storing into the Localities list
                    if (LocResponse != "{}")
                    LocInfo = JsonConvert.DeserializeObject<Locality>(LocResponse);
                }

                if (LocInfo != null)
                {
                    return PartialView("Details", LocInfo);
                }
                return View("Index");
            }
        }

        //public async Task<ActionResult> StatisticFromApi()
        //{
        //    List<Statistic> StInfo = new();
        //    using (var client = new HttpClient())
        //    {
        //        OnClient(client);
        //        HttpResponseMessage Res = await client.GetAsync("api/GetStatistic");
        //        //Checking the response is successful or not which is sent using HttpClient
        //        if (Res.IsSuccessStatusCode)
        //        {
        //            //Storing the response details recieved from web api
        //            var StResponse = Res.Content.ReadAsStringAsync().Result;
        //            //Deserializing the response recieved from web api and storing into the Localities list
        //            StInfo = JsonConvert.DeserializeObject<List<Statistic>>(StResponse);
        //        }
        //        if (StInfo != null)
        //        {
        //            return PartialView("Statistic", StInfo);
        //        }
        //        return View("Index");
        //    }
        //}

        public async Task<ActionResult> Statistic()
        {
            List<Statistic> StInfo = new();

            using (var client = new HttpClient())
            {
                string apiUrlbudget = Baseurl + "api/GetBudgets";
                string apiUrlresidants = Baseurl + "api/GetResidants";
                string apiUrlSquareKm = Baseurl + "api/GetSquareKm";

                // Отправляем GET-запрос к API для получения бюджетов и населения
                HttpResponseMessage budgetResponse = await client.GetAsync(apiUrlbudget);
                HttpResponseMessage residantsResponse = await client.GetAsync(apiUrlresidants);
                HttpResponseMessage squareResponse = await client.GetAsync(apiUrlSquareKm);

                if (budgetResponse.IsSuccessStatusCode && residantsResponse.IsSuccessStatusCode)
                {
                    // Получение ответа в виде строки
                    var budgetResponseString = await budgetResponse.Content.ReadAsStringAsync();
                    var residantsResponseString = await residantsResponse.Content.ReadAsStringAsync();
                    var squareResponseString = await squareResponse.Content.ReadAsStringAsync();

                    // Десериализация ответа
                    double[] Budgets = JsonConvert.DeserializeObject<double[]>(budgetResponseString);
                    double[] Residants = JsonConvert.DeserializeObject<double[]>(residantsResponseString);
                    double[] SquareKm = JsonConvert.DeserializeObject<double[]>(squareResponseString);

                    // URL облачной функции в Yandex Cloud Function
                    string functionUrl = "https://functions.yandexcloud.net/d4eekil0t8rto31d5eov";

                    JObject jsonObject = new JObject(
                        new JProperty("Budgets", new JArray(Budgets)),
                        new JProperty("Residants", new JArray(Residants)),
                        new JProperty("SquareKm", new JArray(SquareKm))
                    );

                    // Content для POST-запроса
                    HttpContent content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");

                    // POST-запрос с данными в теле
                    HttpResponseMessage Res = await client.PostAsync(functionUrl, content);

                    // Проверка успешности ответа
                    if (Res.IsSuccessStatusCode)
                    {
                        // Получение ответа в виде строки
                        var StResponse = await Res.Content.ReadAsStringAsync();

                        // Десериализация ответа
                        var responseJson = JObject.Parse(StResponse);

                        // Извлечение resultBudgets и resultResidants
                        if (responseJson.TryGetValue("resultBudgets", out var result1Token) && result1Token is JArray resultBudgetsArray &&
                            responseJson.TryGetValue("resultResidants", out var result2Token) && result2Token is JArray resultResidantsArray &&
                            responseJson.TryGetValue("resultResidants", out var result3Token) && result2Token is JArray resultSquareArray)
                        {
                            // Преобразование данных result1 и result2 в элементы Statistic и добавление их в StInfo
                            StInfo.AddRange(ConvertJArrayToStatistics(resultBudgetsArray));
                            StInfo.AddRange(ConvertJArrayToStatistics(resultResidantsArray));
                            StInfo.AddRange(ConvertJArrayToStatistics(resultSquareArray));
                        }
                    }

                    if (StInfo != null)
                    {
                        return PartialView("Statistic", StInfo);
                    }
                }

                return View("Index");
            }
        }

        void OnClient(HttpClient client)
        {
            client.BaseAddress = new Uri(Baseurl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ActionResult> LocalityByTable()
        {
            List<Locality> LocInfo = new List<Locality>();
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetLocalities using HttpClient
                HttpResponseMessage Res = await client.GetAsync("api/GetLocalities");
                //Checking the response is successful or not which is sent using HttpClient
                if (Res.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api
                    var LocResponse = Res.Content.ReadAsStringAsync().Result;
                    //Deserializing the response recieved from web api and storing into the Localities list
                    LocInfo = JsonConvert.DeserializeObject<List<Locality>>(LocResponse);
                }

                if (LocInfo != null)
                {
                    return PartialView("LocalityByTable", LocInfo);
                }
                return View("Index");
            }
        }

        public ActionResult Create()
        {
            return PartialView("Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Locality loc)
        {
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetLocalities using HttpClient

                JsonContent content = JsonContent.Create(loc);

                HttpResponseMessage Res = await client.PostAsync("api/CreateLocality", content);
                //Checking the response is successful or not which is sent using HttpClient

                return RedirectToAction("Index");
            }
        }

        public async Task<ActionResult> Update(int id)
        {
            Locality LocInfo = new();
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetLocalities using HttpClient
                HttpResponseMessage Res = await client.GetAsync("api/GetLocalitiyById?id=" + id.ToString());
                //Checking the response is successful or not which is sent using HttpClient
                if (Res.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api
                    var LocResponse = Res.Content.ReadAsStringAsync().Result;
                    //Deserializing the response recieved from web api and storing into the Localities list
                    LocInfo = JsonConvert.DeserializeObject<Locality>(LocResponse);
                }
            }

            if (LocInfo != null)
            {
                return PartialView("Update", LocInfo);
            }
            return View("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Update(Locality loc)
        {
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetLocalities using HttpClient

                JsonContent content = JsonContent.Create(loc);

                HttpResponseMessage Res = await client.PostAsync("api/UpdateLocality", content);
                //Checking the response is successful or not which is sent using HttpClient

                return RedirectToAction("Index");
            }
        }

        public async Task<ActionResult> Delete(int id)
        {
            Locality LocInfo = new();
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetLocalities using HttpClient
                HttpResponseMessage Res = await client.GetAsync("api/GetLocalitiyById?id=" + id.ToString());
                //Checking the response is successful or not which is sent using HttpClient
                if (Res.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api
                    var LocResponse = Res.Content.ReadAsStringAsync().Result;
                    //Deserializing the response recieved from web api and storing into the Localities list
                    LocInfo = JsonConvert.DeserializeObject<Locality>(LocResponse);
                }
            }

            if (LocInfo != null)
            {
                return PartialView("Delete", LocInfo);
            }
            return View("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<ActionResult> DeleteRecord(int id)
        {
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetLocalities using HttpClient

                HttpResponseMessage Res = await client.DeleteAsync("api/DeleteLocality/" + id.ToString());
                //Checking the response is successful or not which is sent using HttpClient

                return RedirectToAction("Index");
            }
        }
    }
}

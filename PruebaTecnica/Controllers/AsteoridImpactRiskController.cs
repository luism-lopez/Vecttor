using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Dynamic;
using System.Linq;

using PruebaTecnica.Models;

namespace PruebaTecnica.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AsteoridImpactRiskController : ControllerBase
    {
        // Usamos HttpClient para hacer llamada a API externa
        private readonly IHttpClientFactory _httpClientFactory;

        // Constructor
        public AsteoridImpactRiskController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}


        [HttpGet("asteroids")] // Ruta de acceso al endpoint
        public async Task<IActionResult> GetList(int days = -1)
        {
            // El número de dias es OBLIGATORIO y debe ser un entero entre 1 y 7
            if (days == -1)
            {
                return BadRequest("Es obligatorio introducir el número de días.");
            }
            
            if (days < 1 || days > 7)
            {
                return BadRequest("El número de días debe ser un entero entre 1 y 7.");
            }

            try
            {
                // Intervalo de tiempo
                DateTime start_date = DateTime.Now;
                DateTime end_date = start_date.AddDays(days);

                // Cliente de conexión
                var httpClient = _httpClientFactory.CreateClient();

                // Llamada a la API
                string apiURL = "https://api.nasa.gov/neo/rest/v1/feed?start_date=" + start_date.ToString("yyyy-MM-dd") + "&end_date=" + end_date.ToString("yyyy-MM-dd") + "&api_key=DEMO_KEY";
                HttpResponseMessage response = await httpClient.GetAsync(apiURL);
                if (response.IsSuccessStatusCode)
                {
                    // Leemos el contenido
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    // Deserializamos el JSON
                    dynamic objResponse = JsonConvert.DeserializeAnonymousType(jsonResponse.ToString(), new ExpandoObject());

                    // Recorremos los datos recogidos
                    List<Asteroid> listAsteroids = new List<Asteroid>();
                    Asteroid objAsteroid;
                    foreach (dynamic objDynamicDia in objResponse.near_earth_objects)
                    {
                        foreach (dynamic objDynamicAsteroid in objDynamicDia.Value)
                        {
                            if (objDynamicAsteroid.is_potentially_hazardous_asteroid == true)
                            {
                                objAsteroid = new Asteroid();
                                objAsteroid.nombre = objDynamicAsteroid.name;
                                objAsteroid.diametro = (objDynamicAsteroid.estimated_diameter.meters.estimated_diameter_min + objDynamicAsteroid.estimated_diameter.meters.estimated_diameter_min) / 2.0;
                                objAsteroid.velocidad = objDynamicAsteroid.close_approach_data[0].relative_velocity.kilometers_per_hour;
                                objAsteroid.fecha = objDynamicAsteroid.close_approach_data[0].close_approach_date;
                                objAsteroid.planeta = objDynamicAsteroid.close_approach_data[0].orbiting_body;

                                listAsteroids.Add(objAsteroid);
                            }
                        }
                    }

                    // Ordenamos de mayor a menor diámetro
                    listAsteroids = listAsteroids.OrderByDescending(a => a.diametro).ToList();

                    // Nos quedamos con el top3
                    listAsteroids = listAsteroids.Take(3).ToList();

                    return Ok(listAsteroids);
                }
                else
                {
                    return BadRequest("Error al obtener los datos.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error al realizar la operación." });
            }
        }
    }
}
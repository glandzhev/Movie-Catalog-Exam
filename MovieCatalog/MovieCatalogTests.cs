using MovieCatalog.DTOs;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Text.Json;

namespace MovieCatalog
{
    public class Tests
    {
        private RestClient client;
        private static string CreatedMovieId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken("gopeto@abv.bg", "123456");
            RestClientOptions options = new RestClientOptions("http://144.91.123.158:5000")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:5000");
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });
            RestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        [Order(1)]
        [Test]
        public void Create_New_Movie_With_Required_Fields()
        {
            // Arrange
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(new
            {
                title = "Test Movie",
                description = "Test Description"
            });

            // Act
            var response = client.Execute(request);
            var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content.Movie, Is.Not.Null);
            Assert.That(content.Movie.Id, Is.Not.Null.Or.Empty);
            Assert.That(content.Msg, Is.EqualTo("Movie created successfully!"));

            CreatedMovieId = content.Movie.Id;
        }

        [Order(2)]
        [Test]
        public void Edit_Created_Movie()
        {
            // Arrange
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", CreatedMovieId);
            request.AddJsonBody(new
            {
                title = "Edited Test Movie",
                description = "Edited Test Description"
            });

            // Act
            var response = client.Execute(request);
            var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content.Msg, Is.EqualTo("Movie edited successfully!"));
        }

        [Order(3)]
        [Test]
        public void Get_All_Movies()
        {
            // Arrange
            var request = new RestRequest("/api/Catalog/All", Method.Get);

            // Act
            var response = client.Execute(request);
            var content = JsonSerializer.Deserialize<List<MovieDto>>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Is.Not.Empty);
        }

        [Order(4)]
        [Test]
        public void Delete_Created_Movie()
        {
            // Arrange
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", CreatedMovieId);

            // Act
            var response = client.Execute(request);
            var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content.Msg, Is.EqualTo("Movie deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void Create_Movie_Without_Required_Fields()
        {
            // Arrange
            var request = new RestRequest("/api/Movie/Create", Method.Post);
            request.AddJsonBody(new
            {
                title = "",
                description = ""
            });

            // Act
            var response = client.Execute(request);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void Edit_NonExisting_Movie()
        {
            // Arrange
            var request = new RestRequest("/api/Movie/Edit", Method.Put);
            request.AddQueryParameter("movieId", "invalid-movie-id");
            request.AddJsonBody(new
            {
                title = "Edited Test Movie",
                description = "Edited Test Description"
            });

            // Act
            var response = client.Execute(request);
            var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(content.Msg, Is.EqualTo("Unable to edit the movie! Check the movieId parameter or user verification!"));
        }

        [Order(7)]
        [Test]
        public void Delete_NonExisting_Movie()
        {
            // Arrange
            var request = new RestRequest("/api/Movie/Delete", Method.Delete);
            request.AddQueryParameter("movieId", "invalid-movie-id");

            // Act
            var response = client.Execute(request);
            var content = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(content.Msg, Is.EqualTo("Unable to delete the movie! Check the movieId parameter or user verification!"));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}

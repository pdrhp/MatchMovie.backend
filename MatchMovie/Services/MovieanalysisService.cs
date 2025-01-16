using MatchMovie.Interfaces;
using MatchMovie.Models.Entities;
using Newtonsoft.Json;
using OpenAI.Chat;

namespace MatchMovie.Services;

public class MovieAnalysisService : IMovieAnalysisService
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<MovieAnalysisService> _logger;

    public MovieAnalysisService(IConfiguration configuration, ILogger<MovieAnalysisService> logger)
    {
        _chatClient = new ChatClient("gpt-4o-mini", configuration["OpenAI:ApiKey"]);
        _logger = logger;
    }

    public async Task<MovieAnalysisDto> AnalyzeMoviesAsync(List<Movie> movies, Dictionary<string, List<int>> votes, Room room)
    {
        try
        {
            var topMovie = movies
                .Select(m => new 
                {
                    Movie = m,
                    VoteCount = votes.Values.Count(v => v.Contains(m.Id))
                })
                .OrderByDescending(m => m.VoteCount)
                .First();

            var moviesInfo = movies
                .Where(m => m.Id != topMovie.Movie.Id) 
                .Select(m => new
                {
                    m.Title,
                    m.Overview,
                    m.Genres,
                    VoteCount = votes.Values.Count(v => v.Contains(m.Id)),
                    Voters = votes.Where(v => v.Value.Contains(m.Id))
                        .Select(v => room.ParticipantNames[v.Key])
                        .ToList()
                }).ToList();

            var voterPatterns = votes.Select(v => new
            {
                ParticipantId = v.Key,
                ParticipantName = room.ParticipantNames[v.Key],
                VotedMovies = v.Value.Select(movieId =>
                    movies.First(m => m.Id == movieId).Title).ToList(),
                PreferredGenres = v.Value
                    .SelectMany(movieId => movies.First(m => m.Id == movieId).Genres)
                    .GroupBy(g => g)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .ToList()
            });

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(@"Você é um especialista em análise cinematográfica. 
                    Forneça análises objetivas e diretas, baseadas nos dados fornecidos. 
                    Considere apenas compatibilidades significativas, sempre acima de 60%."),
                new UserChatMessage($@"Analise estes filmes e padrões de votação:

                    FILME EXCLUÍDO DA ANÁLISE (mais votado):
                    {topMovie.Movie.Title} ({topMovie.VoteCount} votos)

                    DADOS DOS FILMES PARA ANÁLISE:
                    {string.Join("\n\n", moviesInfo.Select(m =>
                        $"Título: {m.Title}\n" +
                        $"Gêneros: {string.Join(", ", m.Genres)}\n" +
                        $"Total de Votos: {m.VoteCount}\n" +
                        $"Votantes: {string.Join(", ", m.Voters)}\n" +
                        $"Sinopse: {m.Overview}"))}

                    PREFERÊNCIAS INDIVIDUAIS:
                    {string.Join("\n", voterPatterns.Select(v =>
                        $"Participante: {v.ParticipantName}\n" +
                        $"Filmes Escolhidos: {string.Join(", ", v.VotedMovies)}\n" +
                        $"Gêneros de Interesse: {string.Join(", ", v.PreferredGenres)}"))}

                    Considere apenas:
                    1. Correspondência direta com gêneros preferidos (mínimo 60% de compatibilidade)
                    2. Similaridade com filmes votados (elementos comuns significativos)
                    3. Elementos comuns nas sinopses (temas principais)
                    4. Padrões claros de votação
                    5. Dados estatísticos concretos

                    IMPORTANTE:
                    - Atribua compatibilidades sempre acima de 60% para garantir recomendações relevantes
                    - Base a compatibilidade em critérios objetivos como:
                      * Porcentagem de gêneros coincidentes
                      * Número de elementos comuns com filmes votados
                      * Similaridade temática com as escolhas do participante

                    Para cada participante, forneça razões objetivas e diretas sobre a compatibilidade, 
                    baseadas apenas nos gêneros e filmes escolhidos.")
            };

            var options = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: "movie_analysis",
                    jsonSchema: BinaryData.FromBytes(JsonSchema),
                    jsonSchemaIsStrict: true)
            };

            var completion = await _chatClient.CompleteChatAsync(messages, options);
            _logger.LogInformation("Análise de filmes concluída com sucesso");
            
            var jsonResponse = completion.Value.Content[0].Text;
            _logger.LogInformation("Resposta do ChatGPT: {Response}", jsonResponse);
            
            return JsonConvert.DeserializeObject<MovieAnalysisDto>(jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao analisar filmes com ChatGPT");
            throw;
        }
    }

    private static readonly byte[] JsonSchema = """
        {
            "type": "object",
            "properties": {
                "analiseEstatistica": {
                    "type": "object",
                    "properties": {
                        "totalVotos": { "type": "number" },
                        "totalParticipantes": { "type": "number" },
                        "distribuicaoVotos": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "filme": { "type": "string" },
                                    "votos": { "type": "number" },
                                    "votantes": { 
                                        "type": "array", 
                                        "items": { "type": "string" }
                                    }
                                },
                                "required": ["filme", "votos", "votantes"],
                                "additionalProperties": false
                            }
                        }
                    },
                    "required": ["totalVotos", "totalParticipantes", "distribuicaoVotos"],
                    "additionalProperties": false
                },
                "recomendacaoFinal": {
                    "type": "object",
                    "properties": {
                        "filmeRecomendado": { "type": "string" },
                        "justificativa": { "type": "string" },
                        "compatibilidadePorParticipante": {
                            "type": "array",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "participante": { "type": "string" },
                                    "compatibilidade": { "type": "number" },
                                    "razao": { "type": "string" }
                                },
                                "required": ["participante", "compatibilidade", "razao"],
                                "additionalProperties": false
                            }
                        }
                    },
                    "required": ["filmeRecomendado", "justificativa", "compatibilidadePorParticipante"],
                    "additionalProperties": false
                }
            },
            "required": ["analiseEstatistica", "recomendacaoFinal"],
            "additionalProperties": false
        }
        """u8.ToArray();
}


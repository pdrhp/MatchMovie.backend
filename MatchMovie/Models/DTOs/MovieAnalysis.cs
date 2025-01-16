public record MovieAnalysisDto
{
    public AnaliseEstatisticaDto AnaliseEstatistica { get; init; }
    public RecomendacaoFinalDto RecomendacaoFinal { get; init; }
}

public record AnaliseEstatisticaDto
{
    public int TotalVotos { get; init; }
    public int TotalParticipantes { get; init; }
    public List<DistribuicaoVotosDto> DistribuicaoVotos { get; init; } = new();
}

public record DistribuicaoVotosDto
{
    public string Filme { get; init; }
    public int Votos { get; init; }
    public List<string> Votantes { get; init; } = new();
}

public record RecomendacaoFinalDto
{
    public string FilmeRecomendado { get; init; }
    public string Justificativa { get; init; }
    public List<CompatibilidadeParticipanteDto> CompatibilidadePorParticipante { get; init; } = new();
}

public record CompatibilidadeParticipanteDto
{
    public string Participante { get; init; }
    public double Compatibilidade { get; init; }
    public string Razao { get; init; }
}
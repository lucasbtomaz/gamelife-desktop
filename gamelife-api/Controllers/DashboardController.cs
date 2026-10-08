using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using gamelife_api.Context;

namespace gamelife_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly GameLifeDbContext _contexto;

        public DashboardController(GameLifeDbContext contexto)
        {
            _contexto = contexto;
        }

        // GET: api/dashboard/stats
        [HttpGet("stats")]
        public async Task<ActionResult<object>> ObterEstatisticas()
        {
            var totalJogos = await _contexto.Jogos.CountAsync();
            var totalPosses = await _contexto.PossesJogos.CountAsync();
            var totalCanais = await _contexto.Canais.CountAsync();
            var totalListaDesejos = await _contexto.ItensListaDesejos.CountAsync();

            var registros = await _contexto.RegistrosJogos.ToListAsync();
            var horasJogadas = registros.Sum(r => r.TempoEmHoras ?? 0);
            var jogosZerados = registros.Count(r => r.FinalizadoEm != null);
            var conquistasObtidas = registros.Sum(r => r.ConquistasObtidas ?? 0);
            var conquistasPossiveis = registros.Sum(r => r.ConquistasPossiveis ?? 0);
            var taxaConclusao = conquistasPossiveis > 0 ? (int)Math.Round(((double)conquistasObtidas / conquistasPossiveis) * 100) : 0;

            return Ok(new
            {
                TotalJogos = totalJogos,
                TotalPosses = totalPosses,
                TotalCanais = totalCanais,
                TotalListaDesejos = totalListaDesejos,
                HorasJogadas = Math.Round(horasJogadas, 1),
                JogosZerados = jogosZerados,
                Conquistas = new
                {
                    Obtidas = conquistasObtidas,
                    Possiveis = conquistasPossiveis,
                    TaxaConclusao = taxaConclusao
                }
            });
        }
    }
}

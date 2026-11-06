import { useEffect, useState, useMemo } from 'react';
import { listGameTrackings } from '../api/gameTracking.js';

const Icon = ({ children, size = 18 }) => (
  <svg aria-hidden="true" width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
    {children}
  </svg>
);

const MESES = [
  { num: 1, nome: 'Janeiro' },
  { num: 2, nome: 'Fevereiro' },
  { num: 3, nome: 'Março' },
  { num: 4, nome: 'Abril' },
  { num: 5, nome: 'Maio' },
  { num: 6, nome: 'Junho' },
  { num: 7, nome: 'Julho' },
  { num: 8, nome: 'Agosto' },
  { num: 9, nome: 'Setembro' },
  { num: 10, nome: 'Outubro' },
  { num: 11, nome: 'Novembro' },
  { num: 12, nome: 'Dezembro' }
];

// Dados demonstrativos caso a API ainda não tenha registros cadastrados
const DADOS_DEMO = [
  { id: 1, titulo: 'The Witcher 3: Wild Hunt', plataforma: 'STEAM', status: 'Zerado', tempoEmHoras: 124.5, conquistasObtidas: 65, conquistasPossiveis: 78, dataStatus: '2026-08-14' },
  { id: 2, titulo: 'Cyberpunk 2077', plataforma: 'PC', status: 'Zerado', tempoEmHoras: 86.0, conquistasObtidas: 38, conquistasPossiveis: 44, dataStatus: '2026-10-25' },
  { id: 3, titulo: 'Elden Ring', plataforma: 'PlayStation 5', status: 'NaoTerminado', tempoEmHoras: 71.0, conquistasObtidas: 30, conquistasPossiveis: 42, dataStatus: '2026-11-02' },
  { id: 4, titulo: 'Baldur\'s Gate 3', plataforma: 'STEAM', status: 'NaoTerminado', tempoEmHoras: 52.0, conquistasObtidas: 34, conquistasPossiveis: 54, dataStatus: '2026-11-03' },
  { id: 5, titulo: 'Hades II', plataforma: 'STEAM', status: 'Zerado', tempoEmHoras: 38.0, conquistasObtidas: 30, conquistasPossiveis: 35, dataStatus: '2026-11-04' },
  { id: 6, titulo: 'Hollow Knight', plataforma: 'Nintendo Switch', status: 'Zerado', tempoEmHoras: 58.0, conquistasObtidas: 63, conquistasPossiveis: 63, dataStatus: '2026-05-18' }
];

export default function PainelRetrospectiva() {
  const [itens, setItens] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [ano, setAno] = useState(2026);
  const [mes, setMes] = useState(11); // Novembro
  const [modo, setModo] = useState('mensal'); // 'mensal' | 'wrapped'
  const [filtroLista, setFiltroLista] = useState('todos'); // 'todos' | 'zerados' | 'jogando'
  const [copiado, setCopiado] = useState(false);

  useEffect(() => {
    listGameTrackings()
      .then((data) => {
        if (Array.isArray(data) && data.length > 0) {
          setItens(data);
        } else {
          setItens(DADOS_DEMO);
        }
      })
      .catch(() => {
        setItens(DADOS_DEMO);
      })
      .finally(() => setCarregando(false));
  }, []);

  // Processar estatísticas baseado no período selecionado
  const estatisticas = useMemo(() => {
    const listaBase = itens.length > 0 ? itens : DADOS_DEMO;

    // Filtro por mês e ano
    const filtrados = listaBase.filter(item => {
      if (!item.dataStatus) return true;
      const data = new Date(item.dataStatus);
      const anoItem = data.getFullYear();
      const mesItem = data.getMonth() + 1;

      if (mes === null) {
        return isNaN(anoItem) || anoItem === ano;
      }
      return (isNaN(anoItem) || anoItem === ano) && (isNaN(mesItem) || mesItem === mes);
    });

    const itensParaUso = filtrados.length > 0 ? filtrados : listaBase;

    const tempoTotalHoras = Number(itensParaUso.reduce((acc, curr) => acc + (Number(curr.tempoEmHoras) || 0), 0).toFixed(1));
    const tempoTotalDias = Number((tempoTotalHoras / 24).toFixed(1));
    const totalConquistas = itensParaUso.reduce((acc, curr) => acc + (Number(curr.conquistasObtidas) || 0), 0);
    const totalConquistasPossiveis = itensParaUso.reduce((acc, curr) => acc + (Number(curr.conquistasPossiveis) || 0), 0);
    const taxaConclusao = totalConquistasPossiveis > 0 ? Math.round((totalConquistas / totalConquistasPossiveis) * 100) : 0;

    // Mais jogados ordenados por tempo
    const maisJogados = [...itensParaUso]
      .sort((a, b) => (Number(b.tempoEmHoras) || 0) - (Number(a.tempoEmHoras) || 0))
      .map((item, index) => ({
        ...item,
        tempo: Number(item.tempoEmHoras) || 0,
        porcentagem: tempoTotalHoras > 0 ? Math.round(((Number(item.tempoEmHoras) || 0) / tempoTotalHoras) * 100) : 0,
        posicao: index + 1
      }));

    const zerados = itensParaUso.filter(i => i.status === 'Zerado');
    const jogando = itensParaUso.filter(i => i.status !== 'Zerado' && i.status !== 'Dropado');

    // Distribuição de plataformas
    const plataformasMap = {};
    itensParaUso.forEach(i => {
      const plat = i.plataforma || 'Outra';
      plataformasMap[plat] = (plataformasMap[plat] || 0) + (Number(i.tempoEmHoras) || 1);
    });
    const plataformasOrdenadas = Object.entries(plataformasMap).sort((a, b) => b[1] - a[1]);
    const plataformaFavorita = plataformasOrdenadas.length > 0 ? plataformasOrdenadas[0][0] : 'Steam';

    // Primeiro e último jogo do ano
    const ordenadosPorData = [...listaBase].filter(i => i.dataStatus).sort((a, b) => new Date(a.dataStatus) - new Date(b.dataStatus));
    const primeiroJogo = ordenadosPorData.length > 0 ? ordenadosPorData[0] : maisJogados[0];
    const ultimoJogo = ordenadosPorData.length > 0 ? ordenadosPorData[ordenadosPorData.length - 1] : maisJogados[maisJogados.length - 1];

    // Persona
    let personaTitulo = 'Gamer Estrategista';
    let personaDescricao = 'Você tem gosto apurado e aprecia jornadas com narrativas ricas e desafios envolventes.';
    if (tempoTotalHoras >= 100) {
      personaTitulo = 'Lenda dos Games';
      personaDescricao = 'Mais de 100 horas acumuladas! Seus mundos virtuais são intensos e marcantes.';
    } else if (taxaConclusao >= 65) {
      personaTitulo = 'Caçador de Platinas';
      personaDescricao = 'Você não apenas joga: você domina cada desafio e busca todas as conquistas.';
    }

    return {
      tempoTotalHoras,
      tempoTotalDias,
      totalConquistas,
      totalConquistasPossiveis,
      taxaConclusao,
      maisJogados,
      itensParaUso,
      zerados,
      jogando,
      plataformaFavorita,
      primeiroJogo,
      ultimoJogo,
      personaTitulo,
      personaDescricao
    };
  }, [itens, ano, mes]);

  const copiarResumo = () => {
    const nomeMes = mes !== null ? MESES.find(m => m.num === mes)?.nome : 'Retrospectiva Anual';
    const texto = `🎮✨ MINHA RETROSPECTIVA GAMELIFE (${nomeMes} / ${ano}) ✨🎮
━━━━━━━━━━━━━━━━━━━━
⏱️ Tempo Total: ${estatisticas.tempoTotalHoras}h (${estatisticas.tempoTotalDias} dias nos jogos)
🏆 Conquistas Desbloqueadas: ${estatisticas.totalConquistas} (${estatisticas.taxaConclusao}% de conclusão)
👑 Jogo Destaque: ${estatisticas.maisJogados[0]?.titulo || 'Nenhum'} (${estatisticas.maisJogados[0]?.tempo}h)
🎭 Persona Gamer: ${estatisticas.personaTitulo}
🎯 Plataforma Favorita: ${estatisticas.plataformaFavorita}

🏆 TOP 3 MAIS JOGADOS:
${estatisticas.maisJogados.slice(0, 3).map((j, i) => `${i + 1}º ${j.titulo} — ${j.tempo}h`).join('\n')}

🚀 Primeiro Jogo do Ano: ${estatisticas.primeiroJogo?.titulo || 'N/A'}
🏁 Último Jogo do Ano: ${estatisticas.ultimoJogo?.titulo || 'N/A'}
━━━━━━━━━━━━━━━━━━━━
Acompanhado via GameLife Desktop 🕹️`;

    navigator.clipboard.writeText(texto);
    setCopiado(true);
    setTimeout(() => setCopiado(false), 2500);
  };

  const listaFiltrada = filtroLista === 'zerados'
    ? estatisticas.zerados
    : filtroLista === 'jogando'
    ? estatisticas.jogando
    : estatisticas.itensParaUso;

  return (
    <main id="retrospectiva" className="retrospectiva-view">
      {/* CABEÇALHO DA RETROSPECTIVA */}
      <header className="page-header">
        <div>
          <span className="eyebrow">REVIEW & SPOTIFY WRAPPED</span>
          <h1>Retrospectiva de Jogos</h1>
          <p>Acompanhe suas estatísticas mensais e a grande retrospectiva anual do seu tempo nos games.</p>
        </div>
        <div className="retrospectiva-actions">
          <button type="button" onClick={copiarResumo} className="btn-wrapped-share">
            <Icon><path d="M4 12v8a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2v-8"/><polyline points="16 6 12 2 8 6"/><line x1="12" y1="2" x2="12" y2="15"/></Icon>
            {copiado ? 'Copiado para o Clipboard!' : 'Compartilhar Wrapped'}
          </button>
        </div>
      </header>

      {/* BARRA DE FILTROS DE PERÍODO */}
      <section className="retrospectiva-toolbar">
        <div className="periodo-chips">
          <span className="toolbar-label">Período:</span>
          <button
            type="button"
            className={`chip-btn ${mes === 11 && ano === 2026 ? 'active' : ''}`}
            onClick={() => { setAno(2026); setMes(11); }}
          >
            Novembro/2026 (Atual)
          </button>
          <button
            type="button"
            className={`chip-btn ${mes === 10 && ano === 2026 ? 'active' : ''}`}
            onClick={() => { setAno(2026); setMes(10); }}
          >
            Outubro/2026
          </button>
          <button
            type="button"
            className={`chip-btn ${mes === 9 && ano === 2026 ? 'active' : ''}`}
            onClick={() => { setAno(2026); setMes(9); }}
          >
            Setembro/2026
          </button>
          <button
            type="button"
            className={`chip-btn highlight ${mes === null && ano === 2026 ? 'active' : ''}`}
            onClick={() => { setAno(2026); setMes(null); }}
          >
            ✨ Retrospectiva Anual 2026
          </button>

          <select
            value={mes === null ? 'ano' : mes}
            onChange={(e) => setMes(e.target.value === 'ano' ? null : Number(e.target.value))}
            className="select-mes-custom"
          >
            <option value="ano">Ano Completo ({ano})</option>
            {MESES.map((m) => (
              <option key={m.num} value={m.num}>
                {m.nome} ({ano})
              </option>
            ))}
          </select>
        </div>

        <div className="mode-toggle">
          <button
            type="button"
            className={`mode-btn ${modo === 'mensal' ? 'active' : ''}`}
            onClick={() => setModo('mensal')}
          >
            🎧 Review Mensal
          </button>
          <button
            type="button"
            className={`mode-btn ${modo === 'wrapped' ? 'active' : ''}`}
            onClick={() => setModo('wrapped')}
          >
            ✨ Wrapped Anual
          </button>
        </div>
      </section>

      {/* ========================================= */}
      {/* MODO 1: REVIEW MENSAL                     */}
      {/* ========================================= */}
      {modo === 'mensal' && (
        <div className="retrospectiva-content">
          {/* CARDS DE MÉTRICAS */}
          <div className="dashboard-grid">
            <article className="stat-card">
              <span className="card-kpi-label">Tempo de Jogatina</span>
              <strong>{estatisticas.tempoTotalHoras}h</strong>
              <span>Equivalente a {estatisticas.tempoTotalDias} dias de jogo</span>
            </article>

            <article className="stat-card">
              <span className="card-kpi-label">Conquistas Desbloqueadas</span>
              <strong>{estatisticas.totalConquistas}</strong>
              <span>{estatisticas.taxaConclusao}% de aproveitamento</span>
            </article>

            <article className="stat-card">
              <span className="card-kpi-label">Jogos Ativos no Mês</span>
              <strong>{estatisticas.itensParaUso.length}</strong>
              <span>{estatisticas.zerados.length} zerados • {estatisticas.jogando.length} jogando</span>
            </article>

            <article className="stat-card">
              <span className="card-kpi-label">Jogo Destaque</span>
              <strong className="truncate-title">{estatisticas.maisJogados[0]?.titulo || 'Nenhum'}</strong>
              <span>{estatisticas.maisJogados[0]?.tempo || 0} horas investidas</span>
            </article>
          </div>

          {/* JOGOS MAIS JOGADOS (POR TEMPO) */}
          <section className="retrospectiva-panel">
            <div className="panel-header-clean">
              <div>
                <span className="eyebrow">RANKING DE TEMPO</span>
                <h2>Jogos Mais Jogados</h2>
                <p>Veja onde você mais investiu tempo no período selecionado.</p>
              </div>
            </div>

            <div className="top-played-list">
              {estatisticas.maisJogados.map((jogo, index) => (
                <div key={jogo.id || index} className="top-played-item">
                  <div className={`rank-badge rank-${index + 1}`}>
                    #{index + 1}
                  </div>
                  <div className="top-played-info">
                    <div className="title-row">
                      <strong>{jogo.titulo}</strong>
                      <span className={`status-badge status-${jogo.status || 'NaoTerminado'}`}>
                        {jogo.status === 'Zerado' ? 'Zerado' : 'Jogando'}
                      </span>
                    </div>
                    <small>{jogo.plataforma} • {jogo.conquistasObtidas || 0} troféus conquistados</small>
                  </div>
                  <div className="top-played-time">
                    <strong>{jogo.tempo}h</strong>
                    <div className="progress-bar-wrap">
                      <div className="progress-bar-fill" style={{ width: `${Math.min(jogo.porcentagem || 10, 100)}%` }} />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </section>

          {/* LISTA DE JOGOS NO PERÍODO */}
          <section className="retrospectiva-panel">
            <div className="panel-header-clean">
              <div>
                <span className="eyebrow">HISTÓRICO</span>
                <h2>Jogos no Período</h2>
              </div>
              <div className="category-tabs">
                <button
                  type="button"
                  className={`tab-filter ${filtroLista === 'todos' ? 'active' : ''}`}
                  onClick={() => setFiltroLista('todos')}
                >
                  Todos ({estatisticas.itensParaUso.length})
                </button>
                <button
                  type="button"
                  className={`tab-filter ${filtroLista === 'zerados' ? 'active' : ''}`}
                  onClick={() => setFiltroLista('zerados')}
                >
                  Zerados ({estatisticas.zerados.length})
                </button>
                <button
                  type="button"
                  className={`tab-filter ${filtroLista === 'jogando' ? 'active' : ''}`}
                  onClick={() => setFiltroLista('jogando')}
                >
                  Jogando ({estatisticas.jogando.length})
                </button>
              </div>
            </div>

            <div className="game-grid-cards">
              {listaFiltrada.map((item) => (
                <article key={item.id} className="game-status-card">
                  <div className="card-top">
                    <span className="platform-tag">{item.plataforma}</span>
                    <span className={`status-badge status-${item.status || 'NaoTerminado'}`}>
                      {item.status === 'Zerado' ? 'Zerado' : 'Jogando'}
                    </span>
                  </div>
                  <h3>{item.titulo}</h3>
                  <div className="card-bottom">
                    <span>⏱️ {item.tempoEmHoras || 0} horas</span>
                    <span>🏆 {item.conquistasObtidas || 0} / {item.conquistasPossiveis || 0}</span>
                  </div>
                </article>
              ))}
            </div>
          </section>
        </div>
      )}

      {/* ========================================= */}
      {/* MODO 2: RETROSPECTIVA ANUAL (WRAPPED)     */}
      {/* ========================================= */}
      {modo === 'wrapped' && (
        <div className="wrapped-experience">
          {/* BANNER SPOTIFY WRAPPED */}
          <div className="wrapped-hero-card">
            <div className="wrapped-badge">✨ RETROSPECTIVA GAMELIFE {ano}</div>
            <h2>Seu Ano no Mundo dos Games</h2>
            <p>Cada conquista, cada hora investida e as histórias épicas que você vivenciou em {ano}.</p>

            <div className="persona-card">
              <span className="persona-label">Seu Arquétipo Gamer</span>
              <h3>{estatisticas.personaTitulo}</h3>
              <p>{estatisticas.personaDescricao}</p>
            </div>
          </div>

          {/* PRIMEIRO E ÚLTIMO JOGO DO ANO */}
          <div className="dashboard-platforms">
            <div className="platform-panel wrapped-slide">
              <span className="eyebrow">O PONTO DE PARTIDA</span>
              <h2>🚀 Primeiro Jogo do Ano</h2>
              <div className="wrapped-game-highlight">
                <strong>{estatisticas.primeiroJogo?.titulo || 'Primeiro Jogo'}</strong>
                <p>Plataforma: {estatisticas.primeiroJogo?.plataforma || 'Steam'}</p>
              </div>
            </div>

            <div className="platform-panel wrapped-slide">
              <span className="eyebrow">A JORNADA MAIS RECENTE</span>
              <h2>🏁 Último Jogo do Ano</h2>
              <div className="wrapped-game-highlight">
                <strong>{estatisticas.ultimoJogo?.titulo || 'Último Jogo'}</strong>
                <p>Status: {estatisticas.ultimoJogo?.status === 'Zerado' ? 'Zerado com glória' : 'Em andamento'}</p>
              </div>
            </div>
          </div>

          {/* PLATAFORMA FAVORITA & HORAS TOTAIS */}
          <div className="yearly-panel wrapped-summary-panel">
            <h2>🏆 Resumo Anual de Destaques</h2>
            <div className="year-grid">
              <div className="year-stat">
                <span>Horas Totais</span>
                <strong>{estatisticas.tempoTotalHoras}h</strong>
              </div>
              <div className="year-stat">
                <span>Dias de Jogatina</span>
                <strong>{estatisticas.tempoTotalDias}d</strong>
              </div>
              <div className="year-stat">
                <span>Plataforma Favorita</span>
                <strong>{estatisticas.plataformaFavorita}</strong>
              </div>
              <div className="year-stat">
                <span>Total de Conquistas</span>
                <strong>{estatisticas.totalConquistas}</strong>
              </div>
            </div>

            <div style={{ marginTop: '24px', textAlign: 'center' }}>
              <button type="button" onClick={copiarResumo} className="btn-wrapped-share">
                {copiado ? 'Texto Copiado!' : 'Copiar Resumo da Retrospectiva'}
              </button>
            </div>
          </div>
        </div>
      )}
    </main>
  );
}

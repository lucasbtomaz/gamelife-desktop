import { useEffect, useState, useMemo, useRef } from 'react';
import html2canvas from 'html2canvas';
import { listGameTrackings } from '../api/gameTracking.js';
import { formatarPercentual, limitarPercentual } from '../utils/percentual.js';

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

function obterPeriodo(data = new Date()) {
  return { ano: data.getFullYear(), mes: data.getMonth() + 1 };
}

function obterPeriodoAnterior({ ano, mes }) {
  return mes === 1 ? { ano: ano - 1, mes: 12 } : { ano, mes: mes - 1 };
}

function extrairPeriodoData(valor) {
  if (!valor) return null;
  const correspondencia = String(valor).match(/^(\d{4})-(\d{2})/);
  if (correspondencia) {
    return { ano: Number(correspondencia[1]), mes: Number(correspondencia[2]) };
  }

  const data = new Date(valor);
  if (Number.isNaN(data.getTime())) return null;
  return { ano: data.getFullYear(), mes: data.getMonth() + 1 };
}

function rotuloStatus(status) {
  if (status === 'Zerado') return 'Zerado';
  if (status === 'Dropado') return 'Dropado';
  return 'Em andamento';
}

export default function PainelRetrospectiva() {
  const periodoAtual = useMemo(() => obterPeriodo(), []);
  const periodoAnterior = useMemo(() => obterPeriodoAnterior(periodoAtual), [periodoAtual]);
  const [itens, setItens] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [ano, setAno] = useState(periodoAtual.ano);
  const [mes, setMes] = useState(periodoAtual.mes);
  const [modo, setModo] = useState('mensal');
  const [copiado, setCopiado] = useState(false);

  // Estados e referência para html2canvas (Spotify Wrapped share card)
  const cartaoWrappedRef = useRef(null);
  const [gerandoImagem, setGerandoImagem] = useState(false);
  const [sucessoDownload, setSucessoDownload] = useState(false);

  useEffect(() => {
    listGameTrackings()
      .then((data) => {
        setItens(Array.isArray(data) ? data : []);
        setErro('');
      })
      .catch(() => {
        setItens([]);
        setErro('Não foi possível carregar a retrospectiva. Tente novamente mais tarde.');
      })
      .finally(() => setCarregando(false));
  }, []);

  // Processar estatísticas baseado no período selecionado
  const estatisticas = useMemo(() => {
    const itensParaUso = itens.filter(item => {
      const periodo = extrairPeriodoData(item.dataStatus);
      if (!periodo || periodo.ano !== ano) return false;
      return mes === null || periodo.mes === mes;
    });

    const tempoTotalHoras = Number(itensParaUso.reduce((acc, curr) => acc + (Number(curr.tempoEmHoras) || 0), 0).toFixed(1));
    const tempoTotalDias = Number((tempoTotalHoras / 24).toFixed(1));
    const totalConquistas = itensParaUso.reduce((acc, curr) => acc + (Number(curr.conquistasObtidas) || 0), 0);

    const zerados = itensParaUso.filter(item => item.status === 'Zerado');
    const jogando = itensParaUso.filter(item => item.status !== 'Zerado');

    // Jogos ordenados por tempo (Top games)
    const maisJogados = [...itensParaUso].sort((a, b) => (Number(b.tempoEmHoras) || 0) - (Number(a.tempoEmHoras) || 0));

    // Plataforma mais jogada
    const contagemPlataformas = itensParaUso.reduce((acc, item) => {
      const plat = item.plataforma || 'Outra';
      acc[plat] = (acc[plat] || 0) + Math.max(0, Number(item.tempoEmHoras) || 0);
      return acc;
    }, {});
    const plataformaFavorita = Object.entries(contagemPlataformas).sort((a, b) => b[1] - a[1])[0]?.[0] || 'Sem dados';

    // Primeiro e último jogo do ano
    const ordenadosPorData = [...itensParaUso].sort((a, b) => {
      const dataA = a.dataStatus ? new Date(a.dataStatus).getTime() : 0;
      const dataB = b.dataStatus ? new Date(b.dataStatus).getTime() : 0;
      return dataA - dataB;
    });

    const primeiroJogo = ordenadosPorData[0] || maisJogados[0];
    const ultimoJogo = ordenadosPorData[ordenadosPorData.length - 1] || maisJogados[maisJogados.length - 1];

    // Persona Gamer dinâmica baseada nas horas e perfil
    let personaTitulo = 'Explorador dos Mundos';
    let personaDescricao = 'Você não tem medo de mergulhar em universos desconhecidos e desbravar cada canto dos seus mapas.';
    if (tempoTotalHoras > 200) {
      personaTitulo = 'Mestre dos Reinos Infinitos';
      personaDescricao = 'Sua dedicação aos games ultrapassa limites e você domina cada mecânica com maestria.';
    } else if (zerados.length >= 4) {
      personaTitulo = 'Completista Implacável';
      personaDescricao = 'Para você, um jogo só acaba quando os créditos finais sobem e os troféus estão na estante.';
    } else if (totalConquistas > 80) {
      personaTitulo = 'Caçador de Troféus';
      personaDescricao = 'Nenhum desafio secreto ou conquista rara escapa da sua determinação.';
    }

    return {
      tempoTotalHoras,
      tempoTotalDias,
      totalConquistas,
      zerados,
      jogando,
      maisJogados,
      plataformaFavorita,
      primeiroJogo,
      ultimoJogo,
      personaTitulo,
      personaDescricao,
      itensParaUso
    };
  }, [itens, ano, mes]);

  const copiarResumo = () => {
    const rotuloPeriodo = mes ? `${MESES.find(m => m.num === mes)?.nome}/${ano}` : `Retrospectiva Anual ${ano}`;
    const texto = `🎮✨ MINHA RETROSPECTIVA GAMELIFE (${rotuloPeriodo}) ✨🎮
━━━━━━━━━━━━━━━━━━━━
⏱️ Tempo Total: ${estatisticas.tempoTotalHoras}h jogadas (${estatisticas.tempoTotalDias} dias)
🏆 Conquistas Desbloqueadas: ${estatisticas.totalConquistas}
🎯 Plataforma Favorita: ${estatisticas.plataformaFavorita}
🎭 Persona Gamer: ${estatisticas.personaTitulo}

👑 TOP JOGOS:
${estatisticas.maisJogados.slice(0, 3).map((j, i) => `${i + 1}º ${j.titulo} (${j.tempoEmHoras}h)`).join('\n')}

🚀 Primeiro do Ano: ${estatisticas.primeiroJogo?.titulo || 'N/A'}
🏁 Último do Ano: ${estatisticas.ultimoJogo?.titulo || 'N/A'}
━━━━━━━━━━━━━━━━━━━━
Acompanhado via GameLife Desktop 🕹️`;

    navigator.clipboard.writeText(texto);
    setCopiado(true);
    setTimeout(() => setCopiado(false), 2500);
  };

  // Função para baixar cartão de compartilhamento Spotify Wrapped usando html2canvas
  const baixarCartaoImagem = async () => {
    if (modo !== 'wrapped') {
      setModo('wrapped');
      await new Promise(r => setTimeout(r, 300));
    }

    if (!cartaoWrappedRef.current) return;

    setGerandoImagem(true);
    setSucessoDownload(false);

    try {
      const elemento = cartaoWrappedRef.current;
      const canvas = await html2canvas(elemento, {
        backgroundColor: '#070a13',
        scale: 2,
        useCORS: true,
        logging: false
      });

      const dataUrl = canvas.toDataURL('image/png');
      const link = document.createElement('a');
      link.href = dataUrl;
      link.download = `retrospectiva-gamelife-${ano}.png`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);

      setSucessoDownload(true);
      setTimeout(() => setSucessoDownload(false), 4500);
    } catch (err) {
      console.error('Erro ao gerar imagem com html2canvas:', err);
    } finally {
      setGerandoImagem(false);
    }
  };

  const periodoSelecionado = mes
    ? `${MESES.find(item => item.num === mes)?.nome}/${ano}`
    : `Retrospectiva Anual ${ano}`;

  return (
    <main id="retrospectiva" className="retrospectiva-container">
      <header className="page-header retrospectiva-header">
        <div className="retrospectiva-title-block">
          <span className="eyebrow">RETROSPECTIVA & REVIEW DE JOGATINA</span>
          <h1>Review do Gamer ✨</h1>
          <p>Veja seus jogos mais jogados, horas dedicadas e conquistas do período.</p>
        </div>

        <div className="retrospectiva-actions">
          <button
            type="button"
            onClick={baixarCartaoImagem}
            disabled={gerandoImagem}
            className="btn-wrapped-share btn-wrapped-download"
            title="Baixar imagem em PNG da retrospectiva anual"
          >
            <Icon><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3"/></Icon>
            {gerandoImagem ? 'Gerando Imagem...' : 'Baixar Cartão (PNG)'}
          </button>

          <button
            type="button"
            onClick={copiarResumo}
            className="btn-wrapped-share"
            title="Copiar resumo em texto"
          >
            <Icon><rect x="9" y="9" width="13" height="13" rx="2" ry="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/></Icon>
            {copiado ? 'Texto Copiado!' : 'Compartilhar Texto'}
          </button>
        </div>
      </header>

      <div className="retrospectiva-toolbar">
        <div className="period-selectors">
          <span className="period-label">Período:</span>
          <button
            type="button"
            className={`period-btn ${ano === periodoAtual.ano && mes === periodoAtual.mes ? 'active' : ''}`}
            onClick={() => { setAno(periodoAtual.ano); setMes(periodoAtual.mes); setModo('mensal'); }}
          >
            {MESES.find(item => item.num === periodoAtual.mes)?.nome}/{periodoAtual.ano} (Atual)
          </button>
          <button
            type="button"
            className={`period-btn ${ano === periodoAnterior.ano && mes === periodoAnterior.mes ? 'active' : ''}`}
            onClick={() => { setAno(periodoAnterior.ano); setMes(periodoAnterior.mes); setModo('mensal'); }}
          >
            {MESES.find(item => item.num === periodoAnterior.mes)?.nome}/{periodoAnterior.ano}
          </button>
          <button
            type="button"
            className={`period-btn wrapped-btn ${ano === periodoAtual.ano && mes === null ? 'active' : ''}`}
            onClick={() => { setAno(periodoAtual.ano); setMes(null); setModo('wrapped'); }}
          >
            Retrospectiva Anual {periodoAtual.ano}
          </button>
        </div>

        <div className="mode-toggle">
          <button
            type="button"
            className={`mode-btn ${modo === 'mensal' ? 'active' : ''}`}
            onClick={() => {
              setModo('mensal');
              if (mes === null) {
                setAno(periodoAtual.ano);
                setMes(periodoAtual.mes);
              }
            }}
          >
            Review do Período
          </button>
          <button
            type="button"
            className={`mode-btn ${modo === 'wrapped' ? 'active' : ''}`}
            onClick={() => { setModo('wrapped'); setAno(periodoAtual.ano); setMes(null); }}
            aria-label="Retrospectiva Anual"
          >
            Wrapped
          </button>
        </div>
      </div>

      {carregando && (
        <div className="empty-state">
          <span className="loader" />
          <h3>Calculando suas estatísticas e horas de jogo...</h3>
        </div>
      )}

      {!carregando && erro && (
        <div className="feedback erro page-feedback retrospectiva-feedback" role="alert">
          {erro}
        </div>
      )}

      {/* FEEDBACK DE DOWNLOAD BEM-SUCEDIDO */}
      {sucessoDownload && (
        <div className="feedback sucesso page-feedback" role="alert" style={{ marginBottom: '20px' }}>
          ✨ Imagem da retrospectiva gerada com sucesso via html2canvas (retrospectiva-gamelife-{ano}.png)!
        </div>
      )}

      {/* ========================================= */}
      {/* MODO 1: REVIEW MENSAL / PERÍODO           */}
      {/* ========================================= */}
      {modo === 'mensal' && !carregando && !erro && (
        <div className="retrospectiva-content">
          {estatisticas.itensParaUso.length === 0 ? (
            <div className="empty-state retrospectiva-empty">
              <h3>Nenhum jogo registrado neste período.</h3>
              <p>Os jogos aparecerão aqui quando houver registros em {periodoSelecionado}.</p>
            </div>
          ) : (
            <>
              <div className="retrospectiva-metrics-grid" aria-label="Indicadores do período">
                <article className="retrospectiva-metric metric-purple">
                  <span>Tempo de jogatina</span>
                  <strong>{estatisticas.tempoTotalHoras} horas</strong>
                  <small>Equivalente a {estatisticas.tempoTotalDias} dias</small>
                </article>
                <article className="retrospectiva-metric metric-amber">
                  <span>Conquistas desbloqueadas</span>
                  <strong>{estatisticas.totalConquistas}</strong>
                  <small>Somadas em todos os jogos</small>
                </article>
                <article className="retrospectiva-metric metric-cyan">
                  <span>Biblioteca ativa</span>
                  <strong>{estatisticas.itensParaUso.length} jogos</strong>
                  <small>{estatisticas.zerados.length} zerados · {estatisticas.jogando.length} em andamento</small>
                </article>
                <article className="retrospectiva-metric metric-emerald">
                  <span>Top 1 do período</span>
                  <strong>{estatisticas.maisJogados[0]?.titulo}</strong>
                  <small>{estatisticas.maisJogados[0]?.tempoEmHoras || 0} horas · {estatisticas.plataformaFavorita}</small>
                </article>
              </div>

              <section
                className="retrospectiva-ranking-panel"
                aria-label="Jogos mais jogados no período"
              >
                <div className="retrospectiva-ranking-header">
                  <div>
                    <span className="eyebrow">CLASSIFICAÇÃO DE HORAS</span>
                    <h2>🏆 Jogos mais jogados no período</h2>
                    <p>Ranking dos jogos em que você mais investiu tempo em {periodoSelecionado}.</p>
                  </div>
                  <span className="retrospectiva-total">Total: {estatisticas.itensParaUso.length} jogos</span>
                </div>

                <div className="retrospectiva-ranking-list">
                  {estatisticas.maisJogados.map((item, indice) => {
                    const percentualTempo = estatisticas.tempoTotalHoras > 0
                      ? limitarPercentual(((Number(item.tempoEmHoras) || 0) / estatisticas.tempoTotalHoras) * 100)
                      : 0;

                    return (
                      <article
                        key={item.id}
                        className="retrospectiva-ranking-item"
                        aria-label={`#${indice + 1} ${item.titulo}`}
                      >
                        <span className={`retrospectiva-position position-${indice + 1}`}>#{indice + 1}</span>
                        <div className="retrospectiva-game-icon" aria-hidden="true">🎮</div>
                        <div className="retrospectiva-game-main">
                          <div className="retrospectiva-game-title-row">
                            <h3>{item.titulo}</h3>
                            <span className={`retrospectiva-status status-${item.status || 'NaoTerminado'}`}>
                              {rotuloStatus(item.status)}
                            </span>
                          </div>
                          <p>{item.plataforma || 'Plataforma não informada'}</p>
                        </div>
                        <div className="retrospectiva-achievements">
                          <span>Conquistas</span>
                          <strong>🏆 {item.conquistasObtidas || 0} / {item.conquistasPossiveis || 0}</strong>
                        </div>
                        <div className="retrospectiva-time">
                          <strong>◷ {item.tempoEmHoras || 0}h</strong>
                          <span>{formatarPercentual(percentualTempo)} do tempo</span>
                          <div className="retrospectiva-time-track" aria-hidden="true">
                            <span style={{ width: `${percentualTempo}%` }} />
                          </div>
                        </div>
                      </article>
                    );
                  })}
                </div>
              </section>
            </>
          )}
        </div>
      )}

      {/* ========================================= */}
      {/* MODO 2: RETROSPECTIVA ANUAL (WRAPPED)     */}
      {/* ========================================= */}
      {modo === 'wrapped' && !carregando && !erro && (
        estatisticas.itensParaUso.length === 0 ? (
          <div className="empty-state retrospectiva-empty">
            <h3>Nenhum jogo registrado neste período.</h3>
            <p>Os jogos aparecerão aqui quando houver registros em {periodoSelecionado}.</p>
          </div>
        ) : (
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

            <div style={{ marginTop: '24px', display: 'flex', justifyContent: 'center', gap: '12px', flexWrap: 'wrap' }}>
              <button
                type="button"
                onClick={baixarCartaoImagem}
                disabled={gerandoImagem}
                className="btn-wrapped-share btn-wrapped-download"
              >
                <Icon><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3"/></Icon>
                {gerandoImagem ? 'Gerando Imagem...' : 'Baixar Cartão de Compartilhamento (PNG)'}
              </button>

              <button type="button" onClick={copiarResumo} className="btn-wrapped-share">
                {copiado ? 'Texto Copiado!' : 'Copiar Resumo da Retrospectiva'}
              </button>
            </div>
          </div>

          {/* ========================================================= */}
          {/* CARTÃO SPOTIFY WRAPPED PERSONALIZADO (ALVO DO HTML2CANVAS)*/}
          {/* ========================================================= */}
          <div className="wrapped-card-preview-section">
            <div className="preview-heading">
              <span className="eyebrow">COMPARTILHAMENTO NAS REDES</span>
              <h2>Cartão Spotify Wrapped Personalizado</h2>
              <p>Baixe este cartão personalizado gerado com <strong>html2canvas</strong> para compartilhar nos seus stories do Instagram, WhatsApp ou X.</p>
            </div>

            <div className="wrapped-card-wrapper">
              <div
                ref={cartaoWrappedRef}
                className="spotify-wrapped-card"
                style={{ backgroundColor: '#070a13' }}
              >
                {/* Header com marca GameLife */}
                <div className="card-brand-row">
                  <div className="brand-group">
                    <span className="brand-icon">🎮</span>
                    <div>
                      <strong className="brand-name">GAMELIFE</strong>
                      <span className="brand-sub">RETROSPECTIVA OFICIAL</span>
                    </div>
                  </div>
                  <span className="year-badge">WRAPPED {ano}</span>
                </div>

                {/* Persona Gamer do Cartão */}
                <div className="card-persona-box">
                  <span className="persona-tag">SEU ARQUÉTIPO GAMER</span>
                  <h3 className="persona-title">{estatisticas.personaTitulo}</h3>
                  <p className="persona-desc">{estatisticas.personaDescricao}</p>
                </div>

                {/* Métricas Principais */}
                <div className="card-metrics-grid">
                  <div className="metric-box">
                    <span className="metric-val">{estatisticas.tempoTotalHoras}h</span>
                    <span className="metric-lbl">Jogadas</span>
                  </div>
                  <div className="metric-box">
                    <span className="metric-val">{estatisticas.totalConquistas}</span>
                    <span className="metric-lbl">Conquistas</span>
                  </div>
                  <div className="metric-box">
                    <span className="metric-val">{estatisticas.zerados.length}</span>
                    <span className="metric-lbl">Zerados</span>
                  </div>
                </div>

                {/* Top Jogos do Ano */}
                <div className="card-top-games">
                  <div className="top-games-header">
                    <span>👑 TOP JOGOS DO ANO</span>
                    <span>{ano}</span>
                  </div>
                  <div className="top-games-list">
                    {estatisticas.maisJogados.slice(0, 4).map((jogo, idx) => (
                      <div key={jogo.id || idx} className="top-game-row">
                        <span className={`game-rank rank-${idx + 1}`}>{idx + 1}</span>
                        <span className="game-name">{jogo.titulo}</span>
                        <span className="game-hours">{jogo.tempoEmHoras || 0}h</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Primeiro e Último jogo */}
                <div className="card-milestones-row">
                  <div className="milestone-box">
                    <span className="milestone-label">🚀 1º Jogo</span>
                    <strong className="milestone-val">{estatisticas.primeiroJogo?.titulo || 'Primeiro'}</strong>
                  </div>
                  <div className="milestone-box">
                    <span className="milestone-label">🏁 Último Jogo</span>
                    <strong className="milestone-val">{estatisticas.ultimoJogo?.titulo || 'Último'}</strong>
                  </div>
                </div>

                {/* Rodapé e Equalizador Sonoro Spotify */}
                <div className="card-footer-row">
                  <span className="footer-tag">gamelife.app • review gamer</span>
                  <div className="soundwave-bar">
                    <span style={{ height: '10px' }} />
                    <span style={{ height: '18px' }} />
                    <span style={{ height: '8px' }} />
                    <span style={{ height: '22px' }} />
                    <span style={{ height: '14px' }} />
                    <span style={{ height: '20px' }} />
                    <span style={{ height: '12px' }} />
                    <span style={{ height: '16px' }} />
                  </div>
                </div>
              </div>
            </div>

            <div style={{ textAlign: 'center', marginTop: '18px' }}>
              <button
                type="button"
                onClick={baixarCartaoImagem}
                disabled={gerandoImagem}
                className="btn-wrapped-share btn-wrapped-download"
              >
                <Icon><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M7 10l5 5 5-5M12 15V3"/></Icon>
                {gerandoImagem ? 'Processando Captura...' : 'Baixar Imagem do Cartão (PNG)'}
              </button>
              <p style={{ fontSize: '11px', color: '#94a3b8', marginTop: '8px' }}>
                Resolução 2x via html2canvas • Perfeito para Instagram Stories e Discord
              </p>
            </div>
          </div>
        </div>
        )
      )}
    </main>
  );
}

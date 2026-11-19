import { useMemo, useState } from 'react';
import { ehCatalogo } from '../utils/acesso.js';
import { formatarBadgeAssinatura } from '../api/assinaturas.js';

const jogosPorPagina = 24;
const compararTitulo = new Intl.Collator('pt-BR', { numeric: true, sensitivity: 'base' }).compare;
const ordenarJogos = {
  'titulo-asc': (a, b) => compararTitulo(a.titulo, b.titulo),
  'titulo-desc': (a, b) => compararTitulo(b.titulo, a.titulo),
  'ano-desc': (a, b) => (b.anoLancamento ?? 0) - (a.anoLancamento ?? 0) || compararTitulo(a.titulo, b.titulo)
};

const normalize = (value) => (value ?? '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
const formatHours = (hours) => hours == null ? 'Não informado' : `${Number(hours).toLocaleString('pt-BR')} h`;
const formatAchievements = (tracking) => tracking?.conquistasPossiveis == null
  ? 'Não informado'
  : `${tracking.conquistasObtidas ?? 0} / ${tracking.conquistasPossiveis}`;
const statusLabels = { Zerado: 'Zerado', Dropado: 'Dropado', NaoTerminado: 'Em andamento' };
const cardHue = (title) => [...(title ?? '')].reduce((total, char) => total + char.charCodeAt(0), 0) % 360;

const estadosDeAcesso = {
  BloqueadoSemAssinatura: { rotulo: 'Assinatura inativa', classe: 'access-blocked' },
  JogavelPorResgate: { rotulo: 'Jogável com a assinatura', classe: 'access-playable' },
  SaiuDoCatalogo: { rotulo: 'Saiu do catálogo', classe: 'access-blocked' }
};

const badgesDeAcesso = (entry, assinaturas) => {
  const badges = [];
  const compraDigital = entry.compraDigital ?? (!entry.midiaFisica && !entry.porAssinatura);
  if (compraDigital) {
    badges.push({ chave: 'compra-digital', rotulo: 'Compra digital', classe: 'access-digital' });
  }
  if (entry.midiaFisica) {
    badges.push({ chave: 'midia-fisica', rotulo: 'Mídia física', classe: 'access-physical' });
  }
  if (entry.resgatadoDeAssinatura) {
    badges.push({
      chave: 'resgate',
      rotulo: entry.servicoExigido ? `Resgatado · ${entry.servicoExigido}` : 'Resgatado',
      classe: 'access-redeemed'
    });
  } else if (entry.porAssinatura) {
    if (assinaturas.length > 0) {
      for (const assinatura of assinaturas) {
        badges.push({
          chave: `assinatura-${assinatura.servico}`,
          rotulo: formatarBadgeAssinatura(assinatura),
          classe: 'access-subscription'
        });
      }
    } else {
      badges.push({ chave: 'assinatura', rotulo: 'Assinatura', classe: 'access-subscription' });
    }
  }
  const estado = estadosDeAcesso[entry.estadoAcesso];
  if (estado) badges.push({ chave: 'estado', ...estado });
  return badges;
};

export default function LibraryScreen({ items, loading, onEdit }) {
  const [exibicao, setExibicao] = useState('lista');
  const [ordem, setOrdem] = useState('titulo-asc');
  const [pagina, setPagina] = useState(1);
  const alterarFiltro = (setter, valor) => {
    setter(valor);
    setPagina(1);
  };
  const [visao, setVisao] = useState('biblioteca');
  const [search, setSearch] = useState('');
  const [platform, setPlatform] = useState('Todas');
  const [physicalOnly, setPhysicalOnly] = useState(false);

  const platforms = useMemo(() => [...new Set(items.flatMap((item) =>
    (item.plataformas ?? []).map((entry) => entry.nome)))]
    .sort((a, b) => a.localeCompare(b, 'pt-BR')), [items]);

  const filteredGames = useMemo(() => items
    .filter((item) => normalize(item.titulo).includes(normalize(search)))
    .map((item) => ({
      ...item,
      visiblePlatforms: (item.plataformas ?? []).filter((entry) =>
        (visao === 'todos' || ehCatalogo(entry) === (visao === 'catalogo')) &&
        (platform === 'Todas' || entry.nome === platform) &&
        (!physicalOnly || entry.midiaFisica))
    }))
    .filter((item) => item.visiblePlatforms.length > 0)
    .sort(ordenarJogos[ordem]), [items, search, platform, physicalOnly, visao, ordem]);

  const totalPaginas = Math.max(1, Math.ceil(filteredGames.length / jogosPorPagina));
  const paginaAtual = Math.min(pagina, totalPaginas);
  const inicio = (paginaAtual - 1) * jogosPorPagina;
  const jogosVisiveis = filteredGames.slice(inicio, inicio + jogosPorPagina);

  return <section className="library-showcase" aria-label="Biblioteca de jogos">
    <div className="library-toolbar">
      <div className="library-display-controls">
        <label className="library-search input-wrap">
          <svg aria-hidden="true" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg>
          <span className="sr-only">Buscar jogos</span>
          <input type="search" aria-label="Buscar jogos" placeholder="Buscar jogo..." value={search} onChange={(event) => alterarFiltro(setSearch, event.target.value)} />
        </label>
        <label>
          Ordenar jogos
          <select value={ordem} onChange={(evento) => alterarFiltro(setOrdem, evento.target.value)}>
            <option value="titulo-asc">Título (A–Z)</option>
            <option value="titulo-desc">Título (Z–A)</option>
            <option value="ano-desc">Lançamentos mais recentes</option>
          </select>
        </label>
        <label>
          Exibição do acervo
          <select value={exibicao} onChange={(evento) => setExibicao(evento.target.value)}>
            <option value="lista">Lista compacta</option>
            <option value="cards">Cards</option>
          </select>
        </label>
      </div>
      <div className="library-filters" aria-label="Filtros da biblioteca">
        <select aria-label="Visão do acervo" value={visao} onChange={(evento) => alterarFiltro(setVisao, evento.target.value)}>
          <option value="biblioteca">Biblioteca</option>
          <option value="catalogo">Catálogo</option>
          <option value="todos">Tudo</option>
        </select>
        <div className="platform-filters">
          {['Todas', ...platforms].map((item) => <button
            type="button"
            key={item}
            className={`btn-ghost btn-compact ${platform === item ? 'active' : ''}`}
            aria-pressed={platform === item}
            onClick={() => alterarFiltro(setPlatform, item)}
          >{item}</button>)}
        </div>
        <label className="physical-filter">
          <input type="checkbox" checked={physicalOnly} onChange={(event) => alterarFiltro(setPhysicalOnly, event.target.checked)} />
          <span>Apenas mídia física</span>
        </label>
      </div>
    </div>

    {loading ? <div className="library-card-empty">Carregando biblioteca...</div> :
      filteredGames.length === 0 ? <div className="library-card-empty">Nenhum jogo encontrado com esses filtros.</div> :
        <div className={exibicao === 'lista' ? 'library-compact-list' : 'library-card-grid'}>
          {jogosVisiveis.map((game) => {
            const assinaturas = game.assinaturas ?? [];
            const algumaPlataformaPorAssinatura = game.visiblePlatforms.some((entry) => entry.porAssinatura);
            return <article
              className="library-game-card library-game-record"
              key={game.jogoId}
              style={{ '--card-hue': cardHue(game.titulo) }}
            >
              <div className="library-card-cover">
                <div className="library-card-tags">
                  <span>{game.anoLancamento ?? 'Ano não informado'}</span>
                  <span>{game.visiblePlatforms.length} {game.visiblePlatforms.length === 1 ? 'plataforma' : 'plataformas'}</span>
                </div>
                <h2>{game.titulo}</h2>
              </div>

              {!algumaPlataformaPorAssinatura && assinaturas.length > 0 && (
                <div className="library-shared-subscriptions" aria-label={`Assinaturas de ${game.titulo}`}>
                  {assinaturas.map((assinatura) => (
                    <span key={assinatura.servico} className="access-badge access-subscription">
                      {formatarBadgeAssinatura(assinatura)}
                    </span>
                  ))}
                </div>
              )}

              <div className="library-platform-list">
                {game.visiblePlatforms.map((entry) => {
                  const accessBadges = badgesDeAcesso(entry, assinaturas);
                  return <section
                    className="library-platform-entry"
                    role="group"
                    aria-label={entry.nome}
                    key={entry.posseId}
                  >
                    <div className="library-platform-head">
                      <strong>{entry.nome}</strong>
                      <div className="library-access-badges">
                        {accessBadges.map((badge) => (
                          <span key={badge.chave} className={`access-badge ${badge.classe}`}>
                            {badge.rotulo}
                          </span>
                        ))}
                      </div>
                    </div>
                    <div className="library-card-metrics">
                      <div><span>Horas</span><strong>{formatHours(entry.acompanhamento?.tempoEmHoras)}</strong></div>
                      <div><span>Conquistas</span><strong>{formatAchievements(entry.acompanhamento)}</strong></div>
                    </div>
                    <div className="library-card-footer">
                      <span className={`library-status status-${entry.acompanhamento?.status ?? 'SemAcompanhamento'}`}>
                        {statusLabels[entry.acompanhamento?.status] ?? 'Sem acompanhamento'}
                      </span>
                    </div>
                  </section>;
                })}
              </div>
              {onEdit && <div className="library-game-actions">
                <button
                  type="button"
                  className="btn-ghost btn-compact"
                  aria-label={`Editar ${game.titulo}`}
                  onClick={() => onEdit(items.find((item) => item.jogoId === game.jogoId) ?? game)}
                >Editar ficha</button>
              </div>}
            </article>;
          })}
        </div>}
    {!loading && totalPaginas > 1 && (
      <nav className="library-pagination" aria-label="Paginação da biblioteca">
        <button type="button" className="btn-ghost btn-compact" disabled={paginaAtual === 1}
          onClick={() => setPagina(paginaAtual - 1)}>Página anterior</button>
        <span role="status">Página {paginaAtual} de {totalPaginas} · {filteredGames.length} jogos</span>
        <button type="button" className="btn-ghost btn-compact" disabled={paginaAtual === totalPaginas}
          onClick={() => setPagina(paginaAtual + 1)}>Próxima página</button>
      </nav>
    )}
  </section>;
}

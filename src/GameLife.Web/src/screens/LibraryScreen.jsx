import { useMemo, useState } from 'react';

const statusLabels = {
  Zerado: 'Zerado',
  Dropado: 'Dropado',
  NaoTerminado: 'Em andamento'
};

function normalize(value) {
  return String(value ?? '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLocaleLowerCase('pt-BR');
}

function cardHue(value) {
  return [...value].reduce((total, character) => total + character.codePointAt(0), 0) % 360;
}

function formatHours(value) {
  return value == null ? 'Não informado' : `${Number(value).toLocaleString('pt-BR')} h`;
}

function formatAchievements(tracking) {
  if (tracking?.conquistasPossiveis == null || tracking?.conquistasObtidas == null) return 'Não informado';
  return `${tracking.conquistasObtidas} / ${tracking.conquistasPossiveis}`;
}

export default function LibraryScreen({ items = [], loading = false }) {
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
        (platform === 'Todas' || entry.nome === platform) &&
        (!physicalOnly || entry.midiaFisica))
    }))
    .filter((item) => item.visiblePlatforms.length > 0), [items, search, platform, physicalOnly]);

  return <section className="library-showcase" aria-label="Biblioteca de jogos">
    <div className="library-toolbar">
      <label className="library-search">
        <svg aria-hidden="true" viewBox="0 0 24 24"><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></svg>
        <span className="sr-only">Buscar jogos</span>
        <input type="search" aria-label="Buscar jogos" placeholder="Buscar jogo..." value={search} onChange={(event) => setSearch(event.target.value)} />
      </label>
      <div className="library-filters" aria-label="Filtros da biblioteca">
        <div className="platform-filters">
          {['Todas', ...platforms].map((item) => <button
            type="button"
            key={item}
            className={platform === item ? 'active' : ''}
            aria-pressed={platform === item}
            onClick={() => setPlatform(item)}
          >{item}</button>)}
        </div>
        <label className="physical-filter">
          <input type="checkbox" checked={physicalOnly} onChange={(event) => setPhysicalOnly(event.target.checked)} />
          <span>Apenas mídia física</span>
        </label>
      </div>
    </div>

    {loading ? <div className="library-card-empty">Carregando biblioteca...</div> :
      filteredGames.length === 0 ? <div className="library-card-empty">Nenhum jogo encontrado com esses filtros.</div> :
        <div className="library-card-grid">
          {filteredGames.map((game) => <article
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

            <div className="library-platform-list">
              {game.visiblePlatforms.map((entry) => <section
                className="library-platform-entry"
                role="group"
                aria-label={entry.nome}
                key={entry.posseId}
              >
                <div className="library-platform-head">
                  <strong>{entry.nome}</strong>
                  <span className="library-platform-media">{entry.midiaFisica ? 'Mídia física' : 'Digital'}</span>
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
              </section>)}
            </div>
          </article>)}
        </div>}
  </section>;
}

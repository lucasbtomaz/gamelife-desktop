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

function cardHue(platform) {
  return [...platform].reduce((total, character) => total + character.codePointAt(0), 0) % 360;
}

function formatHours(value) {
  return value == null ? 'Não informado' : `${Number(value).toLocaleString('pt-BR')} h`;
}

function formatAchievements(item) {
  if (item.conquistasPossiveis == null || item.conquistasObtidas == null) return 'Não informado';
  return `${item.conquistasObtidas} / ${item.conquistasPossiveis}`;
}

export default function LibraryScreen({ items = [], loading = false }) {
  const [search, setSearch] = useState('');
  const [platform, setPlatform] = useState('Todas');
  const [physicalOnly, setPhysicalOnly] = useState(false);
  const flatItems = useMemo(() => items.flatMap((item) => item.plataformas?.map((plataforma) => ({
    id: plataforma.posseId,
    jogoId: item.jogoId,
    titulo: item.titulo,
    anoLancamento: item.anoLancamento,
    plataforma: plataforma.nome,
    midiaFisica: plataforma.midiaFisica,
    status: plataforma.acompanhamento?.status,
    tempoEmHoras: plataforma.acompanhamento?.tempoEmHoras,
    conquistasPossiveis: plataforma.acompanhamento?.conquistasPossiveis,
    conquistasObtidas: plataforma.acompanhamento?.conquistasObtidas
  })) ?? [item]), [items]);
  const platforms = useMemo(() => [...new Set(flatItems.map((item) => item.plataforma))]
    .sort((a, b) => a.localeCompare(b, 'pt-BR')), [flatItems]);
  const filteredItems = useMemo(() => flatItems.filter((item) =>
    normalize(item.titulo).includes(normalize(search)) &&
    (platform === 'Todas' || item.plataforma === platform) &&
    (!physicalOnly || item.midiaFisica)), [flatItems, search, platform, physicalOnly]);

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
      filteredItems.length === 0 ? <div className="library-card-empty">Nenhum jogo encontrado com esses filtros.</div> :
        <div className="library-card-grid">
          {filteredItems.map((item) => <article className="library-game-card" key={item.id} style={{ '--card-hue': cardHue(item.plataforma) }}>
            <div className="library-card-cover">
              <div className="library-card-tags"><span>{item.plataforma}</span><span>{item.midiaFisica ? 'Mídia física' : 'Digital'}</span></div>
              <div><small>{item.anoLancamento ?? 'Não informado'}</small><h2>{item.titulo}</h2></div>
            </div>
            <div className="library-card-metrics">
              <div><span>Horas</span><strong>{formatHours(item.tempoEmHoras)}</strong></div>
              <div><span>Conquistas</span><strong>{formatAchievements(item)}</strong></div>
            </div>
            <div className="library-card-footer">
              <span className={`library-status status-${item.status ?? 'SemAcompanhamento'}`}>
                {statusLabels[item.status] ?? 'Sem acompanhamento'}
              </span>
            </div>
          </article>)}
        </div>}
  </section>;
}

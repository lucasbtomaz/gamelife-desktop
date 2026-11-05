const segments = [
  { key: 'zerados', label: 'Zerados', className: 'summary-completed' },
  { key: 'dropados', label: 'Dropados', className: 'summary-dropped' },
  { key: 'naoTerminados', label: 'Não terminados', className: 'summary-unfinished' }
];

export default function LibraryProgressSummary({ summary }) {
  const total = summary?.total ?? 0;
  return <section className="library-progress-summary" aria-label="Resumo do progresso dos jogos">
    <div className="summary-total"><strong>{total}</strong><span>Total</span></div>
    <div className="summary-segments">
      {segments.map((segment) => {
        const value = summary?.[segment.key] ?? 0;
        return <div
          key={segment.key}
          className={`summary-segment ${segment.className}`}
          style={{ flexGrow: total === 0 ? 1 : value, flexBasis: total === 0 ? 0 : `${Math.max(value, 1)}px` }}
          aria-label={`${value} jogos ${segment.label.toLocaleLowerCase('pt-BR')} de ${total}`}
        ><strong>{value}</strong><span>{segment.label}</span></div>;
      })}
    </div>
  </section>;
}

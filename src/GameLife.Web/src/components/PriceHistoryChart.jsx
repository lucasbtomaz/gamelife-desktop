import { useMemo, useState } from 'react';

const LARGURA = 720;
const ALTURA = 320;
const MARGEM = { topo: 28, direita: 24, base: 58, esquerda: 76 };
const CORES = ['#8b5cf6', '#38bdf8', '#f59e0b', '#22c55e', '#ec4899', '#f97316', '#14b8a6', '#a78bfa'];

const corDaPlataforma = (chave) => {
  const hash = String(chave).split('').reduce((total, caractere) => ((total * 31) + caractere.charCodeAt(0)) | 0, 0);
  return CORES[Math.abs(hash) % CORES.length];
};

const formatarValor = (valor) => Number(valor).toLocaleString('pt-BR', {
  style: 'currency',
  currency: 'BRL'
});

const formatarData = (data) => new Date(data).toLocaleString('pt-BR');
const formatarDataEixo = (data) => new Date(data).toLocaleDateString('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric'
});

function prepararGrafico(registros) {
  const validos = registros
    .map((registro) => ({
      ...registro,
      preco: Number(registro.precoAtual),
      instante: new Date(registro.consultadoEm).getTime()
    }))
    .filter((registro) => Number.isFinite(registro.preco) && Number.isFinite(registro.instante));

  if (validos.length === 0) return null;

  const agrupados = new Map();
  validos.forEach((registro) => {
    const chave = registro.canalId ?? registro.canalNome;
    if (!agrupados.has(chave)) agrupados.set(chave, []);
    agrupados.get(chave).push(registro);
  });

  const series = [...agrupados.entries()].map(([chave, pontos]) => ({
    nome: pontos[0].canalNome,
    cor: corDaPlataforma(chave),
    pontos: pontos.sort((a, b) => a.instante - b.instante || a.id - b.id)
  }));

  const precos = validos.map((registro) => registro.preco);
  const instantes = validos.map((registro) => registro.instante);
  const menorPreco = Math.min(...precos);
  const menores = validos
    .filter((registro) => registro.preco === menorPreco)
    .sort((a, b) => a.instante - b.instante || a.id - b.id);
  const menorId = menores[0].id;

  let instanteMin = Math.min(...instantes);
  let instanteMax = Math.max(...instantes);
  const instanteUnico = instanteMin === instanteMax;
  const instanteReal = instanteMin;
  if (instanteUnico) {
    const dozeHoras = 12 * 60 * 60 * 1000;
    instanteMin -= dozeHoras;
    instanteMax += dozeHoras;
  }

  let precoMin = Math.min(...precos);
  let precoMax = Math.max(...precos);
  const intervaloPreco = precoMax - precoMin;
  const folga = intervaloPreco === 0 ? Math.max(precoMax * 0.1, 1) : intervaloPreco * 0.12;
  precoMin = Math.max(0, precoMin - folga);
  precoMax += folga;

  const larguraUtil = LARGURA - MARGEM.esquerda - MARGEM.direita;
  const alturaUtil = ALTURA - MARGEM.topo - MARGEM.base;
  const escalaX = (instante) => MARGEM.esquerda
    + ((instante - instanteMin) / (instanteMax - instanteMin)) * larguraUtil;
  const escalaY = (preco) => MARGEM.topo
    + (1 - ((preco - precoMin) / (precoMax - precoMin))) * alturaUtil;

  return {
    series: series.map((serie) => ({
      ...serie,
      pontos: serie.pontos.map((registro) => ({
        ...registro,
        x: escalaX(registro.instante),
        y: escalaY(registro.preco),
        menor: registro.id === menorId
      }))
    })),
    ticksX: (instanteUnico ? [instanteReal] : [instanteMin, (instanteMin + instanteMax) / 2, instanteMax]).map((valor) => ({
      valor,
      x: escalaX(valor)
    })),
    ticksY: Array.from({ length: 5 }, (_, indice) => {
      const valor = precoMin + ((precoMax - precoMin) * indice) / 4;
      return { valor, y: escalaY(valor) };
    }).reverse()
  };
}

export default function PriceHistoryChart({ registros = [], tituloJogo }) {
  const grafico = useMemo(() => prepararGrafico(registros), [registros]);
  const [ativo, setAtivo] = useState(null);

  if (!grafico) {
    return <p className="price-chart-empty">Nenhum histórico de preço cadastrado.</p>;
  }

  return (
    <section className="price-history-chart">
      <div className="price-chart-canvas">
        <svg
          className="price-chart-svg"
          viewBox={`0 0 ${LARGURA} ${ALTURA}`}
          role="group"
          aria-roledescription="gráfico de linhas"
          aria-label={`Histórico de preços de ${tituloJogo}`}
        >
          {grafico.ticksY.map((tick) => (
            <g key={tick.valor}>
              <line className="price-chart-grid" x1={MARGEM.esquerda} x2={LARGURA - MARGEM.direita} y1={tick.y} y2={tick.y} />
              <text className="price-chart-axis price-chart-axis-y" x={MARGEM.esquerda - 12} y={tick.y + 4}>{formatarValor(tick.valor)}</text>
            </g>
          ))}

          {grafico.ticksX.map((tick) => (
            <text className="price-chart-axis price-chart-axis-x" key={tick.valor} x={tick.x} y={ALTURA - 20}>
              {formatarDataEixo(tick.valor)}
            </text>
          ))}

          {grafico.series.map((serie) => (
            <g key={serie.nome}>
              {serie.pontos.length > 1 && (
                <polyline
                  aria-label={`Evolução de preços na ${serie.nome}`}
                  className="price-chart-line"
                  fill="none"
                  points={serie.pontos.map((ponto) => `${ponto.x},${ponto.y}`).join(' ')}
                  stroke={serie.cor}
                />
              )}
              {serie.pontos.map((ponto) => {
                const descricao = `${serie.nome}, ${formatarValor(ponto.preco)}, ${formatarData(ponto.consultadoEm)}${ponto.menor ? ', Menor preço histórico' : ''}`;
                return (
                  <circle
                    key={ponto.id}
                    className={`price-chart-point${ponto.menor ? ' price-chart-point-min' : ''}`}
                    cx={ponto.x}
                    cy={ponto.y}
                    r={ponto.menor ? 7 : 5}
                    fill={serie.cor}
                    role="img"
                    tabIndex="0"
                    aria-label={descricao}
                    onFocus={() => setAtivo({ ...ponto, nome: serie.nome })}
                    onBlur={() => setAtivo(null)}
                    onMouseEnter={() => setAtivo({ ...ponto, nome: serie.nome })}
                    onMouseLeave={() => setAtivo(null)}
                  />
                );
              })}
            </g>
          ))}
        </svg>

        {ativo && (
          <div
            className="price-chart-tooltip"
            role="status"
            style={{ left: `${(ativo.x / LARGURA) * 100}%`, top: `${(ativo.y / ALTURA) * 100}%` }}
          >
            <strong>{ativo.nome}</strong>
            <span>{formatarValor(ativo.preco)}</span>
            <time>{formatarData(ativo.consultadoEm)}</time>
            {ativo.menor && <em>Menor preço histórico</em>}
          </div>
        )}
      </div>

      <div className="price-chart-legend" aria-label="Legenda do histórico de preços">
        {grafico.series.map((serie) => (
          <span key={serie.nome}><i style={{ backgroundColor: serie.cor }} />{serie.nome}</span>
        ))}
      </div>
    </section>
  );
}

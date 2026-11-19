import { formatarDataAssinatura } from '../api/assinaturas.js';

const formatarPrazo = (dias) => {
  if (dias === 0) return 'hoje';
  return dias === 1 ? '1 dia' : `${dias} dias`;
};

const valorOuZero = (valor) => Number(valor) || 0;

export const formatarHoras = (horas) => {
  const segundosTotais = Math.round(valorOuZero(horas) * 3600);
  const horasInteiras = Math.floor(segundosTotais / 3600);
  const minutos = Math.floor((segundosTotais % 3600) / 60);
  const segundos = segundosTotais % 60;
  return [horasInteiras, minutos, segundos]
    .map((parte) => String(parte).padStart(2, '0'))
    .join(':');
};

const formatarPercentual = (valor) => `${valorOuZero(valor).toLocaleString('pt-BR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2
})}%`;

const MetricCard = ({ valor, rotulo }) => (
  <article className="stat-card dashboard-stat">
    <strong>{valor}</strong>
    <span>{rotulo}</span>
  </article>
);

const PlatformPanel = ({ titulo, itens = [] }) => (
  <section className="panel-card platform-panel">
    <h2>{titulo}</h2>
    <div className="platform-list">
      {itens.map((item) => (
        <div className="platform-row" key={item.plataforma}>
          <span>{item.plataforma}</span>
          <strong>{item.total}</strong>
        </div>
      ))}
    </div>
  </section>
);

function HomeDashboard({
  dashboard,
  perfil,
  onRemoverDesejo,
  onContinuarMonitorando,
  processandoAlertaId = null
}) {
  const resumo = dashboard?.resumo ?? {};
  const anos = dashboard?.zeradosPorAno ?? [];
  const saidas = dashboard?.alertasSaidaCatalogo ?? [];
  const alertas = dashboard?.alertasListaDesejos ?? [];

  return (
    <>
      {alertas.length > 0 && (
        <section className="home-alerts" aria-label="Alertas da Lista de Desejos">
          {alertas.map((alerta) => {
            const ocupada = processandoAlertaId === alerta.desejoId;
            return (
              <article className="home-alert-card" key={alerta.desejoId}>
                <div className="home-alert-content">
                  <span className="eyebrow">JOGO JÁ NA BIBLIOTECA</span>
                  <h2>{alerta.titulo}</h2>
                  <p>
                    Este jogo já está na sua Biblioteca, mas ainda possui preços monitorados na Lista de Desejos.
                  </p>
                  <div className="home-alert-stores">
                    <span>Lojas ou plataformas monitoradas:</span>
                    {(alerta.lojasMonitoradas ?? []).map((loja) => (
                      <strong key={loja}>{loja}</strong>
                    ))}
                  </div>
                </div>
                <div className="home-alert-actions">
                  <button
                    type="button"
                    className="btn-ghost"
                    disabled={ocupada}
                    onClick={() => onContinuarMonitorando?.(alerta)}
                  >
                    Continuar monitorando
                  </button>
                  <button
                    className="btn-primary" type="button"
                    disabled={ocupada}
                    onClick={() => onRemoverDesejo?.(alerta)}
                  >
                    Remover da Lista de Desejos
                  </button>
                </div>
              </article>
            );
          })}
        </section>
      )}

      <section className="dashboard-profile">
        <div className="profile-card">
          {perfil.fotoDataUrl
            ? <img src={perfil.fotoDataUrl} alt="Foto do perfil" />
            : <span className="large-avatar">{perfil.nome?.slice(0, 2).toUpperCase()}</span>}
          <div><span className="eyebrow">PERFIL LOCAL</span><h2>{perfil.nome || 'Jogador'}</h2></div>
        </div>
      </section>

      {saidas.length > 0 && (
        <section className="home-alerts" aria-label="Saindo do catálogo">
          <h2>Saindo do catálogo</h2>
          {saidas.map((item) => (
            <article className="home-alert-card" key={`${item.jogoId}-${item.servico}`}>
              <div className="home-alert-content">
                <h3>{item.titulo}</h3>
                <p>{item.servico} · {formatarDataAssinatura(item.dataEncerramento?.slice(0, 10))}</p>
              </div>
              <strong>{formatarPrazo(item.diasRestantes)}</strong>
            </article>
          ))}
        </section>
      )}

      <section className="dashboard-metrics" aria-label="Resumo da biblioteca">
        <MetricCard valor={`${valorOuZero(resumo.tempoNaPlataformaAnos)} anos`} rotulo="Tempo na plataforma" />
        <MetricCard valor={valorOuZero(resumo.jogosZerados)} rotulo="Jogos zerados" />
        <MetricCard valor={formatarHoras(resumo.horasZeradas)} rotulo="Horas de zerados" />
        <MetricCard valor={valorOuZero(resumo.totalJogos)} rotulo="Jogos na biblioteca" />
        <MetricCard valor={valorOuZero(resumo.conquistasPossiveis)} rotulo="Conquistas possíveis" />
        <MetricCard valor={valorOuZero(resumo.conquistasObtidas)} rotulo="Conquistas obtidas" />
        <MetricCard valor={formatarPercentual(resumo.percentualConquistas)} rotulo="Percentual de conquistas" />
        <MetricCard valor={valorOuZero(resumo.totalColecoes)} rotulo="Coleções" />
      </section>

      <section className="dashboard-platforms">
        <PlatformPanel titulo="Jogos zerados por plataforma" itens={dashboard?.zeradosPorPlataforma} />
        <PlatformPanel titulo="Jogos na biblioteca por plataforma" itens={dashboard?.bibliotecaPorPlataforma} />
      </section>

      <section className="panel-card yearly-panel">
        <h2>Total zerados por ano</h2>
        <div className="year-grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(70px, 1fr))' }}>
          {anos.map((item) => (
            <article className="year-stat" key={item.ano}>
              <span>{item.ano}</span>
              <strong>{item.total}</strong>
            </article>
          ))}
        </div>
      </section>

      <section className="home-columns">
        {perfil.exibirJogosRecentes && (
          <div className="panel-card">
            <h2>Jogos recentes</h2>
            {dashboard?.jogosRecentes?.length
              ? dashboard.jogosRecentes.map((jogo) => <div className="simple-row" key={`${jogo.jogoId}-${jogo.adquiridoEm}`}><strong>{jogo.titulo}</strong><span>{jogo.plataforma}</span></div>)
              : <p>Nenhum jogo cadastrado ainda.</p>}
          </div>
        )}
        <div className="panel-card">
          <h2>Troféus e conquistas</h2>
          {dashboard?.conquistas?.length
            ? dashboard.conquistas.map((item) => <div className="simple-row" key={item.id}><strong>{item.titulo}</strong><span>{item.tipo}</span></div>)
            : <p>Suas conquistas aparecerão aqui.</p>}
        </div>
      </section>
    </>
  );
}

export default HomeDashboard;

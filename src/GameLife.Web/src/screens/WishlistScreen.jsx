import { useEffect, useMemo, useState } from 'react';
import {
  adicionarPreco,
  atualizarDesejo,
  criarDesejo,
  excluirDesejo,
  listarCanais,
  listarDesejos
} from '../api/wishlist.js';
import {
  excluirAssinatura,
  formatarBadgeAssinatura,
  formatarDataAssinatura,
  salvarAssinatura
} from '../api/assinaturas.js';
import AssinaturaModal from '../components/AssinaturaModal.jsx';
import PriceHistoryChart from '../components/PriceHistoryChart.jsx';
import { filtrarDesejos, formatarOrigens, obterOrigensMenores } from '../utils/wishlist.js';

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const valor = (numero) => numero == null ? 'Sem preço' : moeda.format(numero);

const rotulosSinalizacao = {
  'desconto-80': '80% ou mais de desconto',
  'menor-historico': 'Menor preço histórico',
  'abaixo-20': 'Abaixo de R$ 20',
  'abaixo-50': 'Abaixo de R$ 50'
};

const calcularDesconto = (preco) => {
  const atual = Number(preco.precoAtual);
  const cheio = Number(preco.precoCheio);
  if (!Number.isFinite(atual) || !Number.isFinite(cheio) || cheio <= 0 || atual >= cheio) return null;
  return (1 - (atual / cheio)) * 100;
};

export const classificarCotacao = (preco) => {
  if (!preco.disponivel) return [];
  const sinalizacoes = [];
  const desconto = calcularDesconto(preco);
  const atual = Number(preco.precoAtual);
  if (desconto >= 80) sinalizacoes.push('desconto-80');
  if (preco.menorPrecoHistorico != null && atual === Number(preco.menorPrecoHistorico)) sinalizacoes.push('menor-historico');
  if (atual < 20) sinalizacoes.push('abaixo-20');
  else if (atual < 50) sinalizacoes.push('abaixo-50');
  return sinalizacoes;
};

const situacaoClasse = (situacao) =>
  situacao === 'Disponível'
    ? 'sub-disponivel'
    : situacao === 'Encerrado'
      ? 'sub-encerrado'
      : 'sub-anunciado';

export default function WishlistScreen() {
  const [itens, setItens] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [form, setForm] = useState({ titulo: '', observacao: '' });
  const [precos, setPrecos] = useState({});
  const [edicoes, setEdicoes] = useState({});
  const [canais, setCanais] = useState([]);
  const [abertos, setAbertos] = useState({});
  const [modalAssinatura, setModalAssinatura] = useState(null);
  const [salvandoAssinatura, setSalvandoAssinatura] = useState(false);
  const [busca, setBusca] = useState('');

  useEffect(() => {
    listarDesejos()
      .then(setItens)
      .catch(() => setErro('Não foi possível carregar a Lista de Desejos.'))
      .finally(() => setCarregando(false));
  }, []);

  useEffect(() => {
    listarCanais()
      .then(setCanais)
      .catch(() => {});
  }, []);

  const adicionar = async (evento) => {
    evento.preventDefault();
    setErro('');
    try {
      const item = await criarDesejo({
        titulo: form.titulo.trim(),
        precoDesejado: null,
        observacao: form.observacao.trim() || null
      });
      setItens((atuais) => [...atuais, item].sort((a, b) => a.titulo.localeCompare(b.titulo, 'pt-BR')));
      setForm({ titulo: '', observacao: '' });
    } catch (e) {
      setErro(e.message);
    }
  };

  const salvarPreco = async (evento, item) => {
    evento.preventDefault();
    setErro('');
    const atual = precos[item.id] || {};
    try {
      const atualizado = await adicionarPreco(item.id, {
        nomeCanal: atual.loja?.trim(),
        precoAtual: Number(atual.preco),
        precoCheio: atual.precoCheio === '' || atual.precoCheio == null ? null : Number(atual.precoCheio),
        disponivel: atual.disponivel ?? true
      });
      setItens((lista) => lista.map((valorItem) => valorItem.id === item.id ? atualizado : valorItem));
      setPrecos((todos) => ({ ...todos, [item.id]: {} }));
    } catch {
      setErro('Não foi possível salvar o preço. Tente novamente.');
    }
  };

  const remover = async (item) => {
    if (!window.confirm(`Excluir ${item.titulo} da Lista de Desejos?`)) return;
    try {
      await excluirDesejo(item.id);
      setItens((atuais) => atuais.filter((valorItem) => valorItem.id !== item.id));
    } catch (e) {
      setErro(e.message);
    }
  };

  const salvarDetalhes = async (evento, item) => {
    evento.preventDefault();
    setErro('');
    const edicao = edicoes[item.id] || { observacao: item.observacao ?? '' };
    try {
      const atualizado = await atualizarDesejo(item.id, {
        precoDesejado: item.precoDesejado ?? null,
        observacao: edicao.observacao?.trim() || null
      });
      setItens((lista) => lista.map((valorItem) => valorItem.id === item.id ? atualizado : valorItem));
    } catch {
      setErro('Não foi possível atualizar os detalhes do desejo.');
    }
  };

  const confirmarAssinatura = async (dados) => {
    if (!modalAssinatura) return;
    setSalvandoAssinatura(true);
    try {
      const salva = await salvarAssinatura({
        jogoId: modalAssinatura.item.jogoId,
        titulo: modalAssinatura.item.titulo,
        ...dados
      });
      setItens((atuais) => atuais.map((desejo) => {
        if (desejo.id !== modalAssinatura.item.id) return desejo;
        const existentes = (desejo.assinaturas ?? []).filter(
          (item) => item.id !== salva.id && item.servico !== salva.servico
        );
        const atualizadas = [...existentes, salva].sort((a, b) => a.servico.localeCompare(b.servico, 'pt-BR'));
        return { ...desejo, assinaturas: atualizadas };
      }));
      setModalAssinatura(null);
    } finally {
      setSalvandoAssinatura(false);
    }
  };

  const removerAssinatura = async (desejo, assinatura) => {
    setErro('');
    try {
      await excluirAssinatura(assinatura.id);
      setItens((atuais) => atuais.map((item) => item.id !== desejo.id
        ? item
        : { ...item, assinaturas: (item.assinaturas ?? []).filter((entry) => entry.id !== assinatura.id) }));
    } catch (e) {
      setErro(e.message || 'Não foi possível remover a assinatura.');
    }
  };

  const possuidos = itens.filter((item) => item.jaTenho).length;
  const itensFiltrados = useMemo(() => filtrarDesejos(itens, busca), [itens, busca]);

  return <main id="lista-desejos">
    <header className="page-header">
      <div>
        <span className="eyebrow">BACKLOG DE JOGOS</span>
        <h1>Lista de Desejos</h1>
        <p>Registre por que quer jogar e acompanhe manualmente os preços.</p>
      </div>
      <div className="wishlist-summary">
        <span><strong>{itens.length}</strong> desejos</span>
        <span><strong>{possuidos}</strong> já tenho</span>
      </div>
    </header>
    {erro && <div className="feedback erro page-feedback" role="alert">{erro}</div>}
    <section className="wishlist-layout">
      <form className="form-card wishlist-form" onSubmit={adicionar}>
        <h2>Adicionar jogo</h2>
        <label htmlFor="desejo-titulo">Nome do jogo desejado</label>
        <input id="desejo-titulo" required maxLength="200" value={form.titulo} onChange={(e) => setForm({ ...form, titulo: e.target.value })} />
        <label htmlFor="desejo-observacao">Por que ou quando quero jogar</label>
        <textarea id="desejo-observacao" maxLength="1000" value={form.observacao} onChange={(e) => setForm({ ...form, observacao: e.target.value })} />
        <button className="btn-primary" type="submit">Adicionar desejo</button>
      </form>
      <section className="wishlist-cards">
        {!carregando && itens.length > 0 && <div className="wishlist-search">
          <label htmlFor="busca-desejos">Buscar por jogo ou loja</label>
          <div><input id="busca-desejos" type="search" placeholder="Digite o nome do jogo ou da loja" value={busca} onChange={(evento) => setBusca(evento.target.value)} /><span>{itensFiltrados.length} de {itens.length}</span></div>
        </div>}
        {carregando ? <div className="panel-card">Carregando desejos...</div> : itens.length === 0 ? <div className="panel-card wishlist-empty"><h2>Nenhum jogo na lista</h2><p>Adicione o primeiro jogo usando o formulário.</p></div> : itensFiltrados.length === 0 ? <div className="panel-card wishlist-empty"><h2>Nenhum desejo encontrado</h2><p>Tente buscar por outro jogo ou loja.</p></div> : itensFiltrados.map((item) => {
          const cotacao = precos[item.id] || {};
          const assinaturas = item.assinaturas ?? [];
          const origens = obterOrigensMenores(item);
          return <article className="wishlist-card" key={item.id}>
            <div className="wishlist-card-head">
              <div>
                <h2>{item.titulo}</h2>
                {item.jaTenho && <span className="owned-badge">Já tenho</span>}
              </div>
              <div className="wishlist-card-actions">
                <button
                  type="button"
                  className="btn-ghost btn-compact"
                  aria-label={`Adicionar assinatura para ${item.titulo}`}
                  onClick={() => setModalAssinatura({ item, assinatura: null })}
                >
                  Adicionar assinatura
                </button>
                <button className="danger-link" type="button" onClick={() => remover(item)}>Excluir</button>
              </div>
            </div>

            {assinaturas.length > 0 && (
              <div className="subscription-badges" aria-label={`Assinaturas de ${item.titulo}`}>
                {assinaturas.map((assinatura) => {
                  const dataDisp = formatarDataAssinatura(assinatura.dataDisponibilidade);
                  const dataEnc = formatarDataAssinatura(assinatura.dataEncerramento);
                  const tituloDatas = [
                    dataDisp ? `Entrada: ${dataDisp}` : null,
                    dataEnc ? `Encerramento: ${dataEnc}` : null
                  ].filter(Boolean).join(' · ');

                  return (
                    <span
                      key={assinatura.id ?? assinatura.servico}
                      className={`subscription-badge ${situacaoClasse(assinatura.situacao)}`}
                      title={tituloDatas || undefined}
                    >
                      <span className="subscription-badge-label">{formatarBadgeAssinatura(assinatura)}</span>
                      <button
                        type="button"
                        className="badge-icon-btn"
                        aria-label={`Editar assinatura ${assinatura.servico} de ${item.titulo}`}
                        onClick={() => setModalAssinatura({ item, assinatura })}
                      >
                        Editar
                      </button>
                      <button
                        type="button"
                        className="badge-icon-btn danger"
                        aria-label={`Remover assinatura ${assinatura.servico} de ${item.titulo}`}
                        onClick={() => removerAssinatura(item, assinatura)}
                      >
                        Remover
                      </button>
                    </span>
                  );
                })}
              </div>
            )}

            {item.observacao && <p className="wishlist-note">{item.observacao}</p>}
            <form className="wish-edit-form" onSubmit={(e) => salvarDetalhes(e, item)}>
              <input aria-label={`Editar observação de ${item.titulo}`} placeholder="Por que ou quando jogar" value={edicoes[item.id]?.observacao ?? item.observacao ?? ''} onChange={(e) => setEdicoes({ ...edicoes, [item.id]: { observacao: e.target.value } })} />
              <button className="btn-primary" type="submit">Atualizar detalhes</button>
            </form>
            <div className="price-stats"><span>Menor atual<strong>{valor(item.menorPrecoAtual)}</strong>{origens.atual.length > 0 && <small className="price-origin" aria-label="Loja do menor atual" title={origens.atual.join(', ')}>{formatarOrigens(origens.atual)}</small>}</span><span>Menor histórico<strong>{valor(item.menorPrecoHistorico)}</strong>{origens.historico.length > 0 && <small className="price-origin" aria-label="Loja do menor histórico" title={origens.historico.join(', ')}>{formatarOrigens(origens.historico)}</small>}</span></div>
            <div className="store-prices">{item.precosAtuais?.length ? item.precosAtuais.map((preco) => {
              const desconto = calcularDesconto(preco);
              const sinalizacoes = classificarCotacao(preco);
              return <div className={`store-price-row ${preco.disponivel ? '' : 'is-unavailable'}`} role="group" aria-label={`Cotação ${preco.canalNome}`} key={preco.canalId}>
                <div className="store-price-main">
                  <b>{preco.canalNome}</b>
                  <strong>{preco.disponivel ? valor(preco.precoAtual) : 'Indisponível'}</strong>
                  {preco.precoCheio != null && <del>{valor(preco.precoCheio)}</del>}
                  {preco.disponivel && desconto != null && <span className="discount-value">{Math.round(desconto)}% de desconto</span>}
                </div>
                {sinalizacoes.length > 0 && <div className="deal-badges">
                  {sinalizacoes.map((sinalizacao) => <span className={`deal-badge deal-badge--${sinalizacao}`} key={sinalizacao}>{rotulosSinalizacao[sinalizacao]}</span>)}
                </div>}
              </div>;
            }) : <p>Nenhum preço informado.</p>}</div>
            <form className="price-form" onSubmit={(e) => salvarPreco(e, item)}>
              <input aria-label={`Loja de ${item.titulo}`} list="canais-preco" required placeholder="Loja, ex.: STEAM" value={cotacao.loja || ''} onChange={(e) => setPrecos({ ...precos, [item.id]: { ...cotacao, loja: e.target.value } })} />
              <input aria-label={`Preço atual de ${item.titulo}`} required type="number" min="0" step="0.01" placeholder="Preço atual" value={cotacao.preco || ''} onChange={(e) => setPrecos({ ...precos, [item.id]: { ...cotacao, preco: e.target.value } })} />
              <input aria-label={`Preço cheio de ${item.titulo}`} type="number" min="0" step="0.01" placeholder="Preço cheio" value={cotacao.precoCheio || ''} onChange={(e) => setPrecos({ ...precos, [item.id]: { ...cotacao, precoCheio: e.target.value } })} />
              <label className="availability"><input aria-label={`Disponível em ${item.titulo}`} type="checkbox" checked={cotacao.disponivel ?? true} onChange={(e) => setPrecos({ ...precos, [item.id]: { ...cotacao, disponivel: e.target.checked } })} /> Disponível</label>
              <button className="btn-primary" type="submit" aria-label={`Salvar preço de ${item.titulo}`}>Salvar preço</button>
            </form>
            <button className="btn-ghost btn-compact" type="button" onClick={() => setAbertos({ ...abertos, [item.id]: !abertos[item.id] })}>{abertos[item.id] ? 'Ocultar histórico' : `Ver histórico (${item.historicoPrecos?.length || 0})`}</button>
            {abertos[item.id] && <PriceHistoryChart registros={item.historicoPrecos} tituloJogo={item.titulo} />}
          </article>;
        })}
      </section>
      <datalist id="canais-preco">{canais.map((canal) => <option key={canal.id} value={canal.nome} />)}</datalist>
    </section>

    {modalAssinatura && (
      <AssinaturaModal
        tituloJogo={modalAssinatura.item.titulo}
        assinaturaInicial={modalAssinatura.assinatura}
        salvando={salvandoAssinatura}
        onSalvar={confirmarAssinatura}
        onFechar={() => setModalAssinatura(null)}
      />
    )}
  </main>;
}

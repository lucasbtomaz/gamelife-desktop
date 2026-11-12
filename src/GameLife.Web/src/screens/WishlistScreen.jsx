import { useEffect, useState } from 'react';
import { adicionarPreco, atualizarDesejo, criarDesejo, excluirDesejo, listarCanais, listarDesejos } from '../api/wishlist.js';

const moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });
const valor = (numero) => numero == null ? 'Sem preço' : moeda.format(numero);

const rotulosSinalizacao = {
  'desconto-80': '80% ou mais de desconto',
  'menor-historico': 'Menor preço histórico',
  'abaixo-20': 'Abaixo de R$ 20'
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
  if (desconto >= 80) sinalizacoes.push('desconto-80');
  if (preco.menorPrecoHistorico != null && Number(preco.precoAtual) === Number(preco.menorPrecoHistorico)) sinalizacoes.push('menor-historico');
  if (Number(preco.precoAtual) < 20) sinalizacoes.push('abaixo-20');
  return sinalizacoes;
};

const agruparHistorico = (historico = []) => {
  const grupos = new Map();
  historico.forEach((preco) => {
    const chave = preco.canalId ?? preco.canalNome;
    const grupo = grupos.get(chave) ?? { chave, canalNome: preco.canalNome, precos: [] };
    grupo.precos.push(preco);
    grupos.set(chave, grupo);
  });
  return Array.from(grupos.values())
    .map((grupo) => ({
      ...grupo,
      precos: grupo.precos.sort((a, b) => {
        const diferencaData = new Date(b.consultadoEm).getTime() - new Date(a.consultadoEm).getTime();
        return diferencaData || Number(b.id) - Number(a.id);
      })
    }))
    .sort((a, b) => a.canalNome.localeCompare(b.canalNome, 'pt-BR'));
};

export default function WishlistScreen() {
  const [itens, setItens] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState('');
  const [form, setForm] = useState({ titulo: '', observacao: '' });
  const [precos, setPrecos] = useState({});
  const [edicoes, setEdicoes] = useState({});
  const [canais, setCanais] = useState([]);
  const [abertos, setAbertos] = useState({});

  useEffect(() => { listarDesejos().then(setItens).catch(() => setErro('Não foi possível carregar a Lista de Desejos.')).finally(() => setCarregando(false)); }, []);
  useEffect(() => { listarCanais().then((lista) => setCanais(lista.filter((canal) => canal.permitePreco))).catch(() => {}); }, []);

  const adicionar = async (evento) => {
    evento.preventDefault(); setErro('');
    try {
      const item = await criarDesejo({ titulo: form.titulo.trim(), precoDesejado: null, observacao: form.observacao.trim() || null });
      setItens((atuais) => [...atuais, item].sort((a, b) => a.titulo.localeCompare(b.titulo, 'pt-BR')));
      setForm({ titulo: '', observacao: '' });
    } catch (e) { setErro(e.message); }
  };

  const salvarPreco = async (evento, item) => {
    evento.preventDefault(); setErro('');
    const atual = precos[item.id] || {};
    try {
      const atualizado = await adicionarPreco(item.id, { nomeCanal: atual.loja?.trim(), precoAtual: Number(atual.preco), precoCheio: atual.precoCheio === '' || atual.precoCheio == null ? null : Number(atual.precoCheio), disponivel: atual.disponivel ?? true });
      setItens((lista) => lista.map((valor) => valor.id === item.id ? atualizado : valor));
      setPrecos((todos) => ({ ...todos, [item.id]: {} }));
    } catch { setErro('Não foi possível salvar o preço. Tente novamente.'); }
  };

  const remover = async (item) => {
    if (!window.confirm(`Excluir ${item.titulo} da Lista de Desejos?`)) return;
    try { await excluirDesejo(item.id); setItens((atuais) => atuais.filter((valor) => valor.id !== item.id)); }
    catch (e) { setErro(e.message); }
  };

  const salvarDetalhes = async (evento, item) => {
    evento.preventDefault(); setErro('');
    const edicao = edicoes[item.id] || { observacao: item.observacao ?? '' };
    try {
      const atualizado = await atualizarDesejo(item.id, { precoDesejado: item.precoDesejado ?? null, observacao: edicao.observacao?.trim() || null });
      setItens((lista) => lista.map((valor) => valor.id === item.id ? atualizado : valor));
    } catch { setErro('Não foi possível atualizar os detalhes do desejo.'); }
  };

  const possuidos = itens.filter((item) => item.jaTenho).length;
  const menor = itens.map((item) => item.menorPrecoAtual).filter((preco) => preco != null).sort((a, b) => a - b)[0];
  return <main id="lista-desejos">
    <header className="page-header"><div><span className="eyebrow">BACKLOG DE JOGOS</span><h1>Lista de Desejos</h1><p>Registre por que quer jogar e acompanhe manualmente os preços.</p></div><div className="wishlist-summary"><span><strong>{itens.length}</strong> desejos</span><span><strong>{possuidos}</strong> já tenho</span><span><strong>{valor(menor)}</strong> melhor preço</span></div></header>
    {erro && <div className="feedback erro page-feedback" role="alert">{erro}</div>}
    <section className="wishlist-layout">
      <form className="form-card wishlist-form" onSubmit={adicionar}>
        <h2>Adicionar jogo</h2>
        <label htmlFor="desejo-titulo">Nome do jogo desejado</label><input id="desejo-titulo" required maxLength="200" value={form.titulo} onChange={(e) => setForm({ ...form, titulo: e.target.value })} />
        <label htmlFor="desejo-observacao">Por que ou quando quero jogar</label><textarea id="desejo-observacao" maxLength="1000" value={form.observacao} onChange={(e) => setForm({ ...form, observacao: e.target.value })} />
        <button type="submit">Adicionar desejo</button>
      </form>
      <section className="wishlist-cards">
        {carregando ? <div className="panel-card">Carregando desejos...</div> : itens.length === 0 ? <div className="panel-card wishlist-empty"><h2>Nenhum jogo na lista</h2><p>Adicione o primeiro jogo usando o formulário.</p></div> : itens.map((item) => {
          const cotacao = precos[item.id] || {};
          return <article className="wishlist-card" key={item.id}>
            <div className="wishlist-card-head"><div><h2>{item.titulo}</h2>{item.jaTenho && <span className="owned-badge">Já tenho</span>}</div><button className="danger-link" type="button" onClick={() => remover(item)}>Excluir</button></div>
            {item.observacao && <p className="wishlist-note">{item.observacao}</p>}
            <form className="wish-edit-form" onSubmit={(e) => salvarDetalhes(e, item)}>
              <input aria-label={`Editar observação de ${item.titulo}`} placeholder="Por que ou quando jogar" value={edicoes[item.id]?.observacao ?? item.observacao ?? ''} onChange={(e) => setEdicoes({ ...edicoes, [item.id]: { observacao: e.target.value } })} />
              <button type="submit">Atualizar detalhes</button>
            </form>
            <div className="price-stats"><span>Menor atual<strong>{valor(item.menorPrecoAtual)}</strong></span><span>Menor histórico<strong>{valor(item.menorPrecoHistorico)}</strong></span></div>
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
              <button type="submit" aria-label={`Salvar preço de ${item.titulo}`}>Salvar preço</button>
            </form>
            <button className="history-toggle" type="button" onClick={() => setAbertos({ ...abertos, [item.id]: !abertos[item.id] })}>{abertos[item.id] ? 'Ocultar histórico' : `Ver histórico (${item.historicoPrecos?.length || 0})`}</button>
            {abertos[item.id] && <div className="price-history">{agruparHistorico(item.historicoPrecos).map((grupo) => <section aria-label={`Histórico ${grupo.canalNome}`} className="price-history-store" key={grupo.chave}>
              <h3>{grupo.canalNome}</h3>
              <div className="price-history-list">{grupo.precos.map((preco) => <div className="price-history-entry" key={preco.id}>
                <strong aria-label="Preço atual">{valor(preco.precoAtual)}</strong>
                {preco.precoCheio != null && <span>Preço cheio {valor(preco.precoCheio)}</span>}
                <span className={preco.disponivel ? 'history-available' : 'history-unavailable'}>{preco.disponivel ? 'Disponível' : 'Indisponível'}</span>
                <time dateTime={preco.consultadoEm}>{new Date(preco.consultadoEm).toLocaleString('pt-BR')}</time>
              </div>)}</div>
            </section>)}</div>}
          </article>;
        })}
      </section><datalist id="canais-preco">{canais.map((canal) => <option key={canal.id} value={canal.nome} />)}</datalist>
    </section>
  </main>;
}

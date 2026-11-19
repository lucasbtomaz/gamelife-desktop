import { useEffect, useState } from 'react';
import { listarMinhasAssinaturas, salvarMinhaAssinatura } from '../api/minhasAssinaturas.js';

function LinhaAssinatura({ assinatura }) {
  const { servico } = assinatura;
  const [ativa, setAtiva] = useState(assinatura.ativa);
  const [renovacao, setRenovacao] = useState(assinatura.proximaRenovacao?.slice(0, 10) ?? '');
  const [salvando, setSalvando] = useState(false);
  const [feedback, setFeedback] = useState(null);

  const salvar = async (evento) => {
    evento.preventDefault();
    setSalvando(true);
    setFeedback(null);
    try {
      await salvarMinhaAssinatura({ servico, ativa, proximaRenovacao: renovacao || null });
      setFeedback({ texto: 'Salvo' });
    } catch (erro) {
      setFeedback({ texto: erro.message, erro: true });
    } finally {
      setSalvando(false);
    }
  };

  return (
    <form className="platform-row minha-assinatura" aria-label={servico} onSubmit={salvar}>
      <strong>{servico}</strong>
      <label>
        <input type="checkbox" checked={ativa} disabled={salvando} onChange={(evento) => {
          setAtiva(evento.target.checked);
          setFeedback(null);
        }} />
        Ativa
      </label>
      <label>
        Próxima renovação
        <input type="date" value={renovacao} disabled={salvando} onChange={(evento) => {
          setRenovacao(evento.target.value);
          setFeedback(null);
        }} />
      </label>
      <button className="btn-ghost btn-compact" type="submit" disabled={salvando}>
        {salvando ? 'Salvando…' : 'Salvar'}
      </button>
      {feedback && <span role={feedback.erro ? 'alert' : 'status'}>{feedback.texto}</span>}
    </form>
  );
}

export default function MinhasAssinaturas() {
  const [assinaturas, setAssinaturas] = useState(null);
  const [erro, setErro] = useState('');

  useEffect(() => {
    let ativa = true;
    listarMinhasAssinaturas()
      .then((dados) => {
        if (!Array.isArray(dados)) throw new Error('Não foi possível carregar suas assinaturas.');
        if (ativa) setAssinaturas(dados);
      })
      .catch((falha) => { if (ativa) setErro(falha.message); });
    return () => { ativa = false; };
  }, []);

  return (
    <section className="panel-card minhas-assinaturas" aria-labelledby="minhas-assinaturas-titulo">
      <h2 id="minhas-assinaturas-titulo">Minhas assinaturas</h2>
      {erro && <p role="alert">{erro}</p>}
      {!erro && !assinaturas && <p role="status">Carregando assinaturas…</p>}
      {assinaturas?.map((item) => <LinhaAssinatura key={item.servico} assinatura={item} />)}
    </section>
  );
}

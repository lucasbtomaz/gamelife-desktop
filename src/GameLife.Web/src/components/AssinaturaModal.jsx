import { useState } from 'react';
import { SERVICOS_ASSINATURA, SITUACOES_ASSINATURA } from '../api/assinaturas.js';

const paraInputDate = (valor) => (valor ? String(valor).slice(0, 10) : '');

export default function AssinaturaModal({
  tituloJogo,
  assinaturaInicial = null,
  salvando = false,
  onSalvar,
  onFechar
}) {
  const [servico, setServico] = useState(() => assinaturaInicial?.servico || SERVICOS_ASSINATURA[0]);
  const [situacao, setSituacao] = useState(() => assinaturaInicial?.situacao || SITUACOES_ASSINATURA[1]);
  const [dataDisponibilidade, setDataDisponibilidade] = useState(() =>
    paraInputDate(assinaturaInicial?.dataDisponibilidade)
  );
  const [dataEncerramento, setDataEncerramento] = useState(() =>
    paraInputDate(assinaturaInicial?.dataEncerramento)
  );
  const [erro, setErro] = useState('');

  const editando = Boolean(assinaturaInicial);

  const enviar = async (evento) => {
    evento.preventDefault();
    setErro('');

    if (!servico) {
      setErro('Selecione o serviço da assinatura.');
      return;
    }

    if (!situacao) {
      setErro('Selecione a situação da assinatura.');
      return;
    }

    if (dataDisponibilidade && dataEncerramento && dataEncerramento < dataDisponibilidade) {
      setErro('A data de encerramento não pode ser anterior à data de disponibilidade.');
      return;
    }

    try {
      await onSalvar({
        servico,
        situacao,
        dataDisponibilidade: dataDisponibilidade || null,
        dataEncerramento: dataEncerramento || null
      });
    } catch (e) {
      setErro(e.message || 'Não foi possível salvar a assinatura.');
    }
  };

  return (
    <div className="modal-backdrop" role="presentation" onClick={onFechar}>
      <div
        className="modal-card"
        role="dialog"
        aria-modal="true"
        aria-labelledby="titulo-modal-assinatura"
        onClick={(evento) => evento.stopPropagation()}
      >
        <div className="modal-head">
          <div>
            <span className="eyebrow">{editando ? 'EDITAR ASSINATURA' : 'NOVA ASSINATURA'}</span>
            <h2 id="titulo-modal-assinatura">{tituloJogo}</h2>
          </div>
          <button
            type="button"
            className="modal-close"
            aria-label="Fechar formulário de assinatura"
            onClick={onFechar}
            disabled={salvando}
          >
            ✕
          </button>
        </div>

        <form className="modal-form" onSubmit={enviar}>
          <label htmlFor="assinatura-servico">Serviço</label>
          <select
            id="assinatura-servico"
            value={servico}
            onChange={(evento) => setServico(evento.target.value)}
            disabled={salvando || editando}
          >
            {SERVICOS_ASSINATURA.map((opcao) => (
              <option key={opcao} value={opcao}>{opcao}</option>
            ))}
          </select>

          <label htmlFor="assinatura-situacao">Situação</label>
          <select
            id="assinatura-situacao"
            value={situacao}
            onChange={(evento) => setSituacao(evento.target.value)}
            disabled={salvando}
          >
            {SITUACOES_ASSINATURA.map((opcao) => (
              <option key={opcao} value={opcao}>{opcao}</option>
            ))}
          </select>

          <label htmlFor="assinatura-data-disponibilidade">
            Data prevista ou data de disponibilidade
          </label>
          <input
            id="assinatura-data-disponibilidade"
            type="date"
            value={dataDisponibilidade}
            onChange={(evento) => setDataDisponibilidade(evento.target.value)}
            disabled={salvando}
          />

          <label htmlFor="assinatura-data-encerramento">
            Data de encerramento (quando aplicável)
          </label>
          <input
            id="assinatura-data-encerramento"
            type="date"
            value={dataEncerramento}
            onChange={(evento) => setDataEncerramento(evento.target.value)}
            disabled={salvando}
          />

          {erro && <div className="feedback erro" role="alert">{erro}</div>}

          <div className="modal-actions">
            <button type="button" className="secondary-button" onClick={onFechar} disabled={salvando}>
              Cancelar
            </button>
            <button type="submit" disabled={salvando}>
              {salvando ? 'Salvando...' : 'Salvar assinatura'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

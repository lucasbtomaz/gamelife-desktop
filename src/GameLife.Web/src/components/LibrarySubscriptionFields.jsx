import {
  SERVICOS_ASSINATURA,
  SITUACOES_ASSINATURA,
  formatarBadgeAssinatura,
  formatarDataAssinatura
} from '../api/assinaturas.js';

export default function LibrarySubscriptionFields({
  assinaturas, editorAssinatura, setEditorAssinatura, iniciarEdicao,
  confirmarRascunhoAssinatura, removerAssinaturaDaLista, saving
}) {
  return (
    <fieldset className="platform-details library-subscriptions-box">
      <legend>Assinaturas vinculadas ao jogo</legend>
      <p className="subscription-hint">
        As assinaturas são compartilhadas automaticamente entre Biblioteca e Lista de Desejos.
      </p>

      {assinaturas.length > 0 && (
        <div className="subscription-badges" aria-label="Assinaturas cadastradas para o jogo">
          {assinaturas.map((assinatura) => {
            const entrada = formatarDataAssinatura(assinatura.dataDisponibilidade);
            const encerramento = formatarDataAssinatura(assinatura.dataEncerramento);
            const meta = [entrada ? `Entrada: ${entrada}` : null, encerramento ? `Encerramento: ${encerramento}` : null]
              .filter(Boolean)
              .join(' · ');
            return (
              <span key={assinatura.servico} className="subscription-badge" title={meta || undefined}>
                <span className="subscription-badge-label">{formatarBadgeAssinatura(assinatura)}</span>
                <button
                  type="button"
                  className="badge-icon-btn"
                  aria-label={`Editar assinatura ${assinatura.servico}`}
                  onClick={() => iniciarEdicao(assinatura)}
                  disabled={saving}
                >
                  Editar
                </button>
                <button
                  type="button"
                  className="badge-icon-btn danger"
                  aria-label={`Remover assinatura ${assinatura.servico}`}
                  onClick={() => removerAssinaturaDaLista(assinatura)}
                  disabled={saving}
                >
                  Remover
                </button>
              </span>
            );
          })}
        </div>
      )}

      {editorAssinatura ? (
        <div className="subscription-subform">
          <label htmlFor="bib-assinatura-servico">
            Serviço de assinatura
            <select
              id="bib-assinatura-servico"
              value={editorAssinatura.servico}
              onChange={(evento) => setEditorAssinatura({ ...editorAssinatura, servico: evento.target.value })}
              disabled={saving}
            >
              {SERVICOS_ASSINATURA.map((opcao) => (
                <option key={opcao} value={opcao}>{opcao}</option>
              ))}
            </select>
          </label>

          <label htmlFor="bib-assinatura-situacao">
            Situação da disponibilidade
            <select
              id="bib-assinatura-situacao"
              value={editorAssinatura.situacao}
              onChange={(evento) => setEditorAssinatura({ ...editorAssinatura, situacao: evento.target.value })}
              disabled={saving}
            >
              {SITUACOES_ASSINATURA.map((opcao) => (
                <option key={opcao} value={opcao}>{opcao}</option>
              ))}
            </select>
          </label>

          <label htmlFor="bib-assinatura-entrada">
            Data de entrada
            <input
              id="bib-assinatura-entrada"
              type="date"
              value={editorAssinatura.dataDisponibilidade}
              onChange={(evento) => setEditorAssinatura({ ...editorAssinatura, dataDisponibilidade: evento.target.value })}
              disabled={saving}
            />
          </label>

          <label htmlFor="bib-assinatura-encerramento">
            Data de encerramento
            <input
              id="bib-assinatura-encerramento"
              type="date"
              value={editorAssinatura.dataEncerramento}
              onChange={(evento) => setEditorAssinatura({ ...editorAssinatura, dataEncerramento: evento.target.value })}
              disabled={saving}
            />
          </label>

          <div className="subscription-subform-actions">
            <button
              type="button"
              className="btn-ghost"
              onClick={() => setEditorAssinatura(null)}
              disabled={saving}
            >
              Fechar
            </button>
            <button
              className="btn-primary" type="button"
              onClick={confirmarRascunhoAssinatura}
              disabled={saving}
            >
              Confirmar assinatura
            </button>
          </div>
        </div>
      ) : (
        <button
          type="button"
          className="btn-ghost btn-compact"
          onClick={() => iniciarEdicao()}
          disabled={saving}
        >
          Adicionar assinatura
        </button>
      )}
    </fieldset>
  );
}

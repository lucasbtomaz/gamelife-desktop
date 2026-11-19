import { SERVICOS_ASSINATURA } from '../api/assinaturas.js';

const modos = [
  ['compraDigital', 'Compra digital'],
  ['midiaFisica', 'Mídia física'],
  ['porAssinatura', 'Assinatura'],
  ['resgatadoDeAssinatura', 'Resgatado da assinatura']
];

export default function LibraryPlatformAccess({ plataforma, dados, atualizarPlataforma, saving }) {
  return (
    <fieldset className="platform-details">
      <legend>{plataforma}</legend>
      <div className="access-modes-group" role="group" aria-label={`Formas de acesso em ${plataforma}`}>
        <span className="access-modes-label">Formas de acesso</span>
        <div className="platform-toggles">
          {modos.map(([campo, rotulo]) => (
            <label key={campo}>
              <input type="checkbox" aria-label={`${rotulo} em ${plataforma}`} checked={dados[campo]}
                onChange={(evento) => atualizarPlataforma(plataforma, campo, evento.target.checked)}
                disabled={saving} />
              {rotulo}
            </label>
          ))}
        </div>
      </div>

      {dados.resgatadoDeAssinatura && (
        <label>
          Serviço exigido em {plataforma}
          <select value={dados.servicoExigido} disabled={saving}
            onChange={(evento) => atualizarPlataforma(plataforma, 'servicoExigido', evento.target.value)}>
            <option value="" disabled>Selecione o serviço</option>
            {SERVICOS_ASSINATURA.map((servico) => <option key={servico}>{servico}</option>)}
          </select>
        </label>
      )}
    </fieldset>
  );
}
